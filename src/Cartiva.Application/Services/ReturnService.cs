using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Persistence;
using Cartiva.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Cartiva.Application.Services;

/// <summary>
/// Service for managing return request operations
/// </summary>
public class ReturnService : IReturnService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ReturnService> _logger;

    public ReturnService(ApplicationDbContext db, ILogger<ReturnService> logger)
    {
        _db = db;
        _logger = logger;
    }

    #region Queries

    public async Task<List<ReturnRequest>> GetAllReturnRequestsAsync()
    {
        return await _db.ReturnRequests
            .Include(r => r.ApplicationUser)
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Product)
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.OrderHeader)
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync();
    }

    public async Task<List<ReturnRequest>> GetUserReturnRequestsAsync(string userId)
    {
        return await _db.ReturnRequests
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Product)
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.OrderHeader)
            .Where(r => r.ApplicationUserId == userId)
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync();
    }

    public async Task<ReturnRequest?> GetReturnRequestByIdAsync(int id)
    {
        return await _db.ReturnRequests
            .Include(r => r.ApplicationUser)
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Product)
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.OrderHeader)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<bool> HasExistingReturnAsync(int orderDetailId)
    {
        return await _db.ReturnRequests
            .AnyAsync(r => r.OrderDetailId == orderDetailId
                && (r.Status == SD.ReturnStatusPending || r.Status == SD.ReturnStatusApproved || r.Status == SD.ReturnStatusRefunded));
    }

    #endregion

    #region Customer Operations

    public async Task<ReturnValidationResult> ValidateReturnRequestAsync(string userId, int orderDetailId)
    {
        var orderDetail = await _db.OrderDetails
            .Include(od => od.OrderHeader)
            .Include(od => od.ProductVariant)
                .ThenInclude(pv => pv.Product)
            .Include(od => od.ProductVariant)
                .ThenInclude(pv => pv.SizeValue)
            .FirstOrDefaultAsync(od => od.Id == orderDetailId && od.OrderHeader.ApplicationUserId == userId);

        if (orderDetail == null)
        {
            return ReturnValidationResult.Failure("Order detail not found.");
        }

        if (orderDetail.OrderHeader.OrderStatus != SD.StatusDelivered)
        {
            return ReturnValidationResult.Failure("Returns can only be requested for delivered orders.");
        }

        // Check return window
        var deliveredDate = orderDetail.OrderHeader.OrderDate;
        var shipment = await _db.Shipments
            .FirstOrDefaultAsync(s => s.OrderHeaderId == orderDetail.OrderHeaderId && s.DeliveredDate != null);
        if (shipment?.DeliveredDate != null)
            deliveredDate = shipment.DeliveredDate.Value;

        var daysSinceDelivery = (DateTime.UtcNow - deliveredDate).Days;
        if (daysSinceDelivery > SD.ReturnWindowDays)
        {
            return ReturnValidationResult.Failure($"The {SD.ReturnWindowDays}-day return window has expired.");
        }

        if (await HasExistingReturnAsync(orderDetailId))
        {
            return ReturnValidationResult.Failure("A return request already exists for this item.");
        }

        return ReturnValidationResult.Success(orderDetail, SD.ReturnWindowDays - daysSinceDelivery);
    }

    public async Task<ReturnOperationResult> CreateReturnRequestAsync(string userId, int orderDetailId, string reason, string? description, int quantity)
    {
        var validation = await ValidateReturnRequestAsync(userId, orderDetailId);
        if (!validation.CanReturn)
        {
            return ReturnOperationResult.Failed(validation.ErrorMessage!);
        }

        var orderDetail = validation.OrderDetail!;

        if (quantity < 1 || quantity > orderDetail.Count)
        {
            return ReturnOperationResult.Failed($"Quantity must be between 1 and {orderDetail.Count}.");
        }

        var returnRequest = new ReturnRequest
        {
            OrderDetailId = orderDetailId,
            ApplicationUserId = userId,
            Reason = reason,
            Description = description?.Trim(),
            Quantity = quantity,
            RequestDate = DateTime.UtcNow,
            Status = SD.ReturnStatusPending,
            RefundAmount = orderDetail.Price * quantity
        };

        _db.ReturnRequests.Add(returnRequest);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Return request {ReturnId} created by user {UserId} for order detail {OrderDetailId}",
            returnRequest.Id, userId, orderDetailId);

        return ReturnOperationResult.Succeeded("Return request submitted. We will review it shortly.", returnRequest.Id);
    }

    #endregion

    #region Admin Operations

    public async Task<ReturnOperationResult> ApproveReturnAsync(int id, string? adminNote)
    {
        var returnRequest = await _db.ReturnRequests
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.OrderHeader)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (returnRequest == null)
        {
            return ReturnOperationResult.Failed("Return request not found.");
        }

        returnRequest.Status = SD.ReturnStatusApproved;
        returnRequest.AdminNote = adminNote;
        returnRequest.ResolvedDate = DateTime.UtcNow;

        // Restore stock
        var variant = await _db.ProductVariants.FindAsync(returnRequest.OrderDetail.ProductVariantId);
        if (variant != null)
        {
            variant.Stock += returnRequest.Quantity;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Return request {ReturnId} approved", id);
        return ReturnOperationResult.Succeeded("Return approved. Stock restored. You can now process the refund.");
    }

    public async Task<ReturnOperationResult> RejectReturnAsync(int id, string? adminNote)
    {
        var returnRequest = await _db.ReturnRequests.FindAsync(id);
        if (returnRequest == null)
        {
            return ReturnOperationResult.Failed("Return request not found.");
        }

        returnRequest.Status = SD.ReturnStatusRejected;
        returnRequest.AdminNote = adminNote;
        returnRequest.ResolvedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Return request {ReturnId} rejected", id);
        return ReturnOperationResult.Succeeded("Return request rejected.");
    }

    public async Task<ReturnOperationResult> ProcessRefundAsync(int id)
    {
        var returnRequest = await _db.ReturnRequests
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.OrderHeader)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (returnRequest == null)
        {
            return ReturnOperationResult.Failed("Return request not found.");
        }

        if (returnRequest.Status != SD.ReturnStatusApproved)
        {
            return ReturnOperationResult.Failed("Only approved returns can be refunded.");
        }

        var order = returnRequest.OrderDetail.OrderHeader;
        var refundAmount = returnRequest.RefundAmount ?? (returnRequest.OrderDetail.Price * returnRequest.Quantity);

        // Process Stripe refund if payment was made via Stripe
        if (!string.IsNullOrEmpty(order.PaymentIntentId) &&
            order.PaymentStatus == SD.PaymentStatusApproved)
        {
            try
            {
                var options = new RefundCreateOptions
                {
                    PaymentIntent = order.PaymentIntentId,
                    Amount = (long)(refundAmount * 100)
                };
                var service = new RefundService();
                var refund = await service.CreateAsync(options);

                if (refund.Status == "succeeded" || refund.Status == "pending")
                {
                    returnRequest.RefundId = refund.Id;
                    _logger.LogInformation("Stripe refund {RefundId} for return {ReturnId}, amount {Amount}",
                        refund.Id, id, refundAmount);
                }
                else
                {
                    return ReturnOperationResult.Failed($"Stripe refund status: {refund.Status}. Please try again.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe refund failed for return {ReturnId}", id);
                return ReturnOperationResult.Failed("Refund failed: " + ex.Message);
            }
        }

        returnRequest.Status = SD.ReturnStatusRefunded;
        returnRequest.RefundDate = DateTime.UtcNow;
        returnRequest.RefundAmount = refundAmount;

        // Check if all items are returned/refunded — update order status
        await UpdateOrderStatusIfFullyRefundedAsync(order.Id);

        await _db.SaveChangesAsync();

        _logger.LogInformation("Refund processed for return {ReturnId}, amount {Amount}", id, refundAmount);
        return ReturnOperationResult.Succeeded($"Refund of {refundAmount:C} processed successfully.", id, refundAmount);
    }

    private async Task UpdateOrderStatusIfFullyRefundedAsync(int orderHeaderId)
    {
        var allOrderDetails = await _db.OrderDetails
            .Where(od => od.OrderHeaderId == orderHeaderId)
            .ToListAsync();

        var allDetailIds = allOrderDetails.Select(od => od.Id).ToList();
        var allReturns = await _db.ReturnRequests
            .Where(r => allDetailIds.Contains(r.OrderDetailId) && r.Status == SD.ReturnStatusRefunded)
            .ToListAsync();

        var totalOrderedQty = allOrderDetails.Sum(od => od.Count);
        var totalRefundedQty = allReturns.Sum(r => r.Quantity);

        if (totalRefundedQty >= totalOrderedQty)
        {
            var order = await _db.OrderHeaders.FindAsync(orderHeaderId);
            if (order != null)
            {
                order.OrderStatus = SD.StatusRefunded;
                order.PaymentStatus = SD.PaymentStatusRefunded;
            }
        }
    }

    #endregion

    #region Helpers

    public async Task<int> GetDaysRemainingInReturnWindowAsync(int orderDetailId)
    {
        var orderDetail = await _db.OrderDetails
            .Include(od => od.OrderHeader)
            .FirstOrDefaultAsync(od => od.Id == orderDetailId);

        if (orderDetail == null) return 0;

        var deliveredDate = orderDetail.OrderHeader.OrderDate;
        var shipment = await _db.Shipments
            .FirstOrDefaultAsync(s => s.OrderHeaderId == orderDetail.OrderHeaderId && s.DeliveredDate != null);
        if (shipment?.DeliveredDate != null)
            deliveredDate = shipment.DeliveredDate.Value;

        var daysSinceDelivery = (DateTime.UtcNow - deliveredDate).Days;
        return Math.Max(0, SD.ReturnWindowDays - daysSinceDelivery);
    }

    public List<string> GetReturnReasons()
    {
        return SD.GetReturnReasons().ToList();
    }

    #endregion
}
