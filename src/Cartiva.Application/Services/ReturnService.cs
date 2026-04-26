using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Domain.Extensions;
using Cartiva.Domain.Enums;
using Cartiva.Domain.Interfaces;
using Cartiva.Persistence;
using Cartiva.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;

namespace Cartiva.Application.Services;

public class ReturnService : IReturnService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ReturnService> _logger;
    private readonly ICreditNoteService _creditNoteService;
    private readonly INotificationService _notificationService;
    private readonly IAccountsReceivableAdjustmentService _arAdjustmentService;

    public ReturnService(
        ApplicationDbContext db,
        ILogger<ReturnService> logger,
        ICreditNoteService creditNoteService,
        INotificationService notificationService,
        IAccountsReceivableAdjustmentService arAdjustmentService)
    {
        _db = db;
        _logger = logger;
        _creditNoteService = creditNoteService;
        _notificationService = notificationService;
        _arAdjustmentService = arAdjustmentService;
    }

    #region Queries

    public async Task<List<ReturnRequest>> GetAllReturnRequestsAsync()
        => await _db.ReturnRequests
            .Include(r => r.ApplicationUser)
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Product)
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.OrderHeader)
                    .ThenInclude(oh => oh.ApplicationUser)
                        .ThenInclude(u => u.Company)
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync();

    public async Task<List<ReturnRequest>> GetUserReturnRequestsAsync(string userId)
        => await _db.ReturnRequests
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Product)
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.OrderHeader)
                    .ThenInclude(oh => oh.ApplicationUser)
                        .ThenInclude(u => u.Company)
            .Where(r => r.ApplicationUserId == userId)
            .OrderByDescending(r => r.RequestDate)
            .ToListAsync();

    public async Task<ReturnRequest?> GetReturnRequestByIdAsync(int id)
        => await _db.ReturnRequests
            .Include(r => r.ApplicationUser)
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Product)
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.OrderHeader)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task<bool> HasExistingReturnAsync(int orderDetailId)
        => await _db.ReturnRequests.AnyAsync(r =>
            r.OrderDetailId == orderDetailId &&
            (r.Status == ReturnStatus.Pending ||
             r.Status == ReturnStatus.Approved ||
             r.Status == ReturnStatus.Refunded));

    #endregion

    #region Customer

    public async Task<ReturnValidationResult> ValidateReturnRequestAsync(string userId, int orderDetailId)
    {
        var orderDetail = await _db.OrderDetails
            .Include(o => o.OrderHeader)
            .Include(o => o.ProductVariant)
                .ThenInclude(p => p.Product)
            .FirstOrDefaultAsync(o =>
                o.Id == orderDetailId &&
                o.OrderHeader.ApplicationUserId == userId);

        if (orderDetail == null)
            return ReturnValidationResult.Failure("Order not found.");

        if (orderDetail.OrderHeader.OrderStatus != OrderStatus.Delivered)
            return ReturnValidationResult.Failure("Order not delivered.");

        var deliveredDate = orderDetail.OrderHeader.OrderDate;

        var shipment = await _db.Shipments
            .FirstOrDefaultAsync(s =>
                s.OrderHeaderId == orderDetail.OrderHeaderId &&
                s.DeliveredDate != null);

        if (shipment?.DeliveredDate != null)
            deliveredDate = shipment.DeliveredDate.Value;

        if ((DateTime.UtcNow - deliveredDate).Days > SD.ReturnWindowDays)
            return ReturnValidationResult.Failure("Return window expired.");

        if (await HasExistingReturnAsync(orderDetailId))
            return ReturnValidationResult.Failure("Return already exists.");

        return ReturnValidationResult.Success(orderDetail,
            SD.ReturnWindowDays - (DateTime.UtcNow - deliveredDate).Days);
    }

    public async Task<ReturnOperationResult> CreateReturnRequestAsync(
        string userId,
        int orderDetailId,
        string reason,
        string? description,
        int quantity)
    {
        var validation = await ValidateReturnRequestAsync(userId, orderDetailId);

        if (!validation.CanReturn)
            return ReturnOperationResult.Failed(validation.ErrorMessage!);

        var od = validation.OrderDetail!;

        if (quantity < 1 || quantity > od.Count)
            return ReturnOperationResult.Failed("Invalid quantity.");

        var rr = new ReturnRequest
        {
            OrderDetailId = orderDetailId,
            ApplicationUserId = userId,
            Reason = reason,
            Description = description?.Trim(),
            Quantity = quantity,
            RequestDate = DateTime.UtcNow,
            Status = ReturnStatus.Pending,
            RefundAmount = od.Price * quantity
        };

        _db.ReturnRequests.Add(rr);
        await _db.SaveChangesAsync();

        // Send return request received notification
        var user = await _db.Users.FindAsync(userId);
        if (user?.Email != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.SendAsync(new NotificationRequest(
                        Recipient: user.Email,
                        Type: NotificationType.ReturnRequestReceived,
                        TemplateData: new Dictionary<string, object>
                        {
                            ["returnRequestId"] = rr.Id.ToString(),
                            ["orderDetailId"] = od.Id.ToString(),
                            ["applicationUserId"] = rr.ApplicationUserId,
                            ["customerName"] = string.IsNullOrWhiteSpace(user.Name) ? (user.UserName ?? user.Email ?? string.Empty) : user.Name,
                            ["reason"] = rr.Reason,
                            ["description"] = rr.Description ?? string.Empty,
                            ["orderNumber"] = od.OrderHeaderId.ToString(),
                            ["productName"] = od.ProductVariant?.Product?.Name ?? "Product",
                            ["quantity"] = quantity.ToString(),
                            ["requestDate"] = rr.RequestDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                            ["status"] = rr.Status,
                            ["adminNote"] = rr.AdminNote ?? string.Empty,
                            ["refundAmount"] = rr.RefundAmount?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty
                        },
                        UserId: userId,
                        ReferenceId: rr.Id.ToString(),
                        ReferenceType: "ReturnRequest",
                        Subject: "Return Request Received"
                    ));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send return request notification for return {ReturnId}", rr.Id);
                }
            });
        }

        return ReturnOperationResult.Succeeded("Return created.", rr.Id);
    }

    #endregion

    #region Admin

    public async Task<ReturnOperationResult> ApproveReturnAsync(int id, string? note)
    {
        var rr = await _db.ReturnRequests
            .Include(r => r.OrderDetail)
                .ThenInclude(o => o.OrderHeader)
                    .ThenInclude(h => h.ApplicationUser)
                        .ThenInclude(u => u.Company)
            .Include(r => r.OrderDetail)
                .ThenInclude(o => o.ProductVariant)
                    .ThenInclude(pv => pv.Product)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rr == null)
            return ReturnOperationResult.Failed("Not found.");

        rr.Status = ReturnStatus.Approved;
        rr.AdminNote = note;
        rr.ResolvedDate = DateTime.UtcNow;

        var variant = await _db.ProductVariants.FindAsync(rr.OrderDetail.ProductVariantId);
        if (variant != null)
            variant.Stock += rr.Quantity;

        await _db.SaveChangesAsync();

        // =========================
        // RETURN FLOW ROUTING LOGIC
        // =========================
        var user = rr.OrderDetail.OrderHeader.ApplicationUser;
        var order = rr.OrderDetail.OrderHeader;

        // Check if this is a company order with DEFERRED payment (B2B flow)
        // Company orders paid upfront are treated like individual customers
        bool isCompanyDeferredPayment = 
            user?.CompanyId.HasValue == true && 
            user.Company?.IsActive == true &&
            (order.PaymentStatus == PaymentStatus.Deferred || order.PaymentStatus == PaymentStatus.Pending);

        if (isCompanyDeferredPayment)
        {
            // =========================
            // B2B COMPANY RETURN FLOW: Create AR Adjustment (NOT Credit Note)
            // Company with DEFERRED payment - use AR adjustment
            // Status stays "Approved" - admin must manually apply AR adjustment
            // =========================
            _logger.LogInformation(
                "Return {ReturnId} is for company {CompanyId} with deferred payment, creating AR adjustment",
                id, user.CompanyId);

            try
            {
                // Find invoice for this order
                var invoice = await _db.Invoices
                    .FirstOrDefaultAsync(i => i.OrderHeaderId == rr.OrderDetail.OrderHeaderId);

                if (invoice != null)
                {
                    // Create AR adjustment for B2B company with deferred payment
                    await _arAdjustmentService.CreateFromReturnRequestAsync(
                        returnRequestId: rr.Id,
                        invoiceId: invoice.Id,
                        companyId: user.CompanyId.Value,
                        createdByUserId: null); // Could pass admin user ID if available

                    _logger.LogInformation(
                        "AR adjustment created for company return {ReturnId}. Status remains Approved - awaiting manual application.",
                        id);

                    // Status remains "Approved" - admin must click "Apply AR Adjustment" to complete
                }
                else
                {
                    _logger.LogWarning(
                        "No invoice found for order {OrderId}, cannot create AR adjustment",
                        rr.OrderDetail.OrderHeaderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to create AR adjustment for return {ReturnId}",
                    id);
                // Continue execution - return approval succeeded even if AR adjustment failed
            }
        }
        else
        {
            // =========================
            // B2C OR COMPANY UPFRONT PAYMENT FLOW: Auto-create Credit Note
            // Individual customers OR company orders paid upfront
            // Status remains "Approved" - admin must manually process refund
            // =========================
            if (user?.CompanyId.HasValue == true)
            {
                _logger.LogInformation(
                    "Return {ReturnId} is for company {CompanyId} with UPFRONT payment, auto-creating credit note",
                    id, user.CompanyId);
            }
            else
            {
                _logger.LogInformation(
                    "Return {ReturnId} is for individual customer, auto-creating credit note",
                    id);
            }

            try
            {
                // Auto-create credit note
                var creditNote = await _creditNoteService.CreateFromReturnRequestAsync(rr.Id);

                _logger.LogInformation(
                    "Credit note {CreditNoteNumber} auto-created for return {ReturnId}",
                    creditNote.CreditNoteNumber, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to auto-create credit note for return {ReturnId}",
                    id);
                // Continue execution - return approval succeeded even if credit note failed
            }
        }

        // Send return request approved notification
        if (rr.OrderDetail.OrderHeader.ApplicationUser?.Email != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.SendAsync(new NotificationRequest(
                        Recipient: rr.OrderDetail.OrderHeader.ApplicationUser.Email,
                        Type: NotificationType.ReturnRequestApproved,
                        TemplateData: new Dictionary<string, object>
                        {
                            ["returnRequestId"] = rr.Id.ToString(),
                            ["orderDetailId"] = rr.OrderDetailId.ToString(),
                            ["applicationUserId"] = rr.ApplicationUserId,
                            ["customerName"] = string.IsNullOrWhiteSpace(rr.OrderDetail.OrderHeader.ApplicationUser.Name)
                                ? rr.OrderDetail.OrderHeader.Name
                                : rr.OrderDetail.OrderHeader.ApplicationUser.Name,
                            ["reason"] = rr.Reason,
                            ["description"] = rr.Description ?? string.Empty,
                            ["quantity"] = rr.Quantity.ToString(),
                            ["requestDate"] = rr.RequestDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                            ["status"] = rr.Status,
                            ["resolvedDate"] = rr.ResolvedDate?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty,
                            ["orderNumber"] = rr.OrderDetail.OrderHeaderId.ToString(),
                            ["productName"] = rr.OrderDetail.ProductVariant?.Product?.Name ?? "Product",
                            ["refundAmount"] = rr.RefundAmount?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                            ["refundId"] = rr.RefundId ?? string.Empty,
                            ["refundDate"] = rr.RefundDate?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty,
                            ["adminNote"] = note ?? ""
                        },
                        UserId: rr.ApplicationUserId,
                        ReferenceId: rr.Id.ToString(),
                        ReferenceType: "ReturnRequest",
                        Subject: "Return Request Approved"
                    ));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send return approved notification for return {ReturnId}", id);
                }
            });
        }

        return ReturnOperationResult.Succeeded("Approved.");
    }

    public async Task<ReturnOperationResult> RejectReturnAsync(int id, string? note)
    {
        var rr = await _db.ReturnRequests
            .Include(r => r.ApplicationUser)
            .Include(r => r.OrderDetail)
                .ThenInclude(o => o.OrderHeader)
            .Include(r => r.OrderDetail)
                .ThenInclude(o => o.ProductVariant)
                    .ThenInclude(pv => pv.Product)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (rr == null)
            return ReturnOperationResult.Failed("Not found.");

        rr.Status = ReturnStatus.Rejected;
        rr.AdminNote = note;
        rr.ResolvedDate = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        // Send return request rejected notification
        if (rr.ApplicationUser?.Email != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.SendAsync(new NotificationRequest(
                        Recipient: rr.ApplicationUser.Email,
                        Type: NotificationType.ReturnRequestRejected,
                        TemplateData: new Dictionary<string, object>
                        {
                            ["returnRequestId"] = rr.Id.ToString(),
                            ["orderDetailId"] = rr.OrderDetailId.ToString(),
                            ["applicationUserId"] = rr.ApplicationUserId,
                            ["customerName"] = string.IsNullOrWhiteSpace(rr.ApplicationUser.Name)
                                ? (rr.ApplicationUser.UserName ?? rr.ApplicationUser.Email ?? string.Empty)
                                : rr.ApplicationUser.Name,
                            ["description"] = rr.Description ?? string.Empty,
                            ["quantity"] = rr.Quantity.ToString(),
                            ["requestDate"] = rr.RequestDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                            ["status"] = rr.Status,
                            ["adminNote"] = rr.AdminNote ?? string.Empty,
                            ["resolvedDate"] = rr.ResolvedDate?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty,
                            ["refundAmount"] = rr.RefundAmount?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
                            ["orderNumber"] = rr.OrderDetail?.OrderHeaderId.ToString() ?? string.Empty,
                            ["productName"] = rr.OrderDetail?.ProductVariant?.Product?.Name ?? "Product",
                            ["reason"] = note ?? "Does not meet return policy requirements"
                        },
                        UserId: rr.ApplicationUserId,
                        ReferenceId: rr.Id.ToString(),
                        ReferenceType: "ReturnRequest",
                        Subject: "Return Request Update"
                    ));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send return rejected notification for return {ReturnId}", id);
                }
            });
        }

        return ReturnOperationResult.Succeeded("Rejected.");
    }

    #endregion

    #region Refund (FIXED CORE LOGIC - WORKS FOR ALL CUSTOMERS)

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

        if (returnRequest.Status != ReturnStatus.Approved)
        {
            return ReturnOperationResult.Failed("Only approved returns can be refunded.");
        }

        var order = returnRequest.OrderDetail.OrderHeader;
        var refundAmount = returnRequest.RefundAmount ?? (returnRequest.OrderDetail.Price * returnRequest.Quantity);

        // ==========================================
        // UNIFIED "IS PAID" VALIDATION LOGIC
        // ==========================================
        bool isPaid = false;
        if (!string.IsNullOrEmpty(order.PaymentIntentId))
        {
            // Any order with a Stripe Payment Intent is considered paid for refund purposes.
            // Stripe's API will be the final check.
            isPaid = true;
        }
        else if (order.PaymentStatus == PaymentStatus.Paid)
        {
            // If no Stripe ID, check if it's an invoice-based order marked as 'Paid'.
            isPaid = true;
        }
        else if (order.PaymentStatus == PaymentStatus.Approved)
        {
            // Also consider 'Approved' as a paid status for non-Stripe scenarios if applicable.
            isPaid = true;
        }

        if (!isPaid)
        {
            _logger.LogWarning(
                "Refund blocked for ReturnId {Id}. Order {OrderId} is not in a paid state. PaymentStatus: {Status}, HasPaymentIntent: {HasIntent}",
                id, order.Id, order.PaymentStatus, !string.IsNullOrEmpty(order.PaymentIntentId));

            return ReturnOperationResult.Failed(
                $"Order is not considered paid (current status: {order.PaymentStatus}). Refund not allowed.");
        }

        // ==========================================
        // CREATE CREDIT NOTE (before status change)
        // ==========================================
        try
        {
            await _creditNoteService.CreateFromReturnRequestAsync(returnRequest.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Credit note creation failed for ReturnId {Id}. Refund process stopped.", id);
            return ReturnOperationResult.Failed("Refund failed because credit note could not be created: " + ex.Message);
        }

        // ==========================================
        // PROCESS STRIPE REFUND (if applicable and not already done)
        // ==========================================
        if (!string.IsNullOrEmpty(order.PaymentIntentId) && string.IsNullOrEmpty(returnRequest.RefundId))
        {
            try
            {
                var service = new RefundService();
                var refund = await service.CreateAsync(new RefundCreateOptions
                {
                    PaymentIntent = order.PaymentIntentId,
                    Amount = (long)(refundAmount * 100)
                });

                if (refund.Status != "succeeded" && refund.Status != "pending")
                {
                    return ReturnOperationResult.Failed($"Stripe refund failed with status: {refund.Status}.");
                }
                returnRequest.RefundId = refund.Id;
                _logger.LogInformation("Stripe refund {RefundId} for return {ReturnId} processed.", refund.Id, id);
            }
            catch (Stripe.StripeException sex) when (sex.Message != null && sex.Message.Contains("already been refunded", StringComparison.OrdinalIgnoreCase))
            {
                // Stripe charge is already refunded (from a previous attempt) — treat as success
                _logger.LogWarning("Stripe charge already refunded for ReturnId {Id}. Continuing to finalize return.", id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe refund error for ReturnId {Id}", id);
                // Note: At this point, a credit note exists but the Stripe refund failed. This may require manual intervention.
                return ReturnOperationResult.Failed($"Stripe refund failed: {ex.Message}. A credit note was created but the refund could not be processed automatically.");
            }
        }
        else if (!string.IsNullOrEmpty(order.PaymentIntentId))
        {
            _logger.LogInformation("Stripe refund already processed (RefundId={RefundId}) for ReturnId {Id}. Skipping Stripe call.", returnRequest.RefundId, id);
        }
        else
        {
            _logger.LogInformation("Processing refund for non-Stripe order {OrderId}. Manual money transfer is required.", order.Id);
        }

        // ==========================================
        // FINALIZE RETURN STATUS
        // ==========================================
        returnRequest.Status = ReturnStatus.Refunded;
        returnRequest.RefundDate = DateTime.UtcNow;
        returnRequest.RefundAmount = refundAmount;

        await UpdateOrderStatusIfFullyRefundedAsync(order.Id);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Refund process completed for return {ReturnId}.", id);
        return ReturnOperationResult.Succeeded($"Refund for {refundAmount:C} completed.", id, refundAmount);
    }

    public async Task<ReturnOperationResult> FinalizeARAdjustmentReturnAsync(int id)
    {
        var returnRequest = await _db.ReturnRequests
            .Include(r => r.OrderDetail)
                .ThenInclude(od => od.OrderHeader)
                    .ThenInclude(oh => oh.ApplicationUser)
                        .ThenInclude(u => u.Company)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (returnRequest == null)
        {
            return ReturnOperationResult.Failed("Return request not found.");
        }

        if (returnRequest.Status != ReturnStatus.Approved)
        {
            return ReturnOperationResult.Failed("Only approved returns can be finalized.");
        }

        // Verify this is a company deferred payment return
        var isCompanyDeferred = returnRequest.OrderDetail?.OrderHeader?.ApplicationUser?.CompanyId != null &&
                               returnRequest.OrderDetail?.OrderHeader?.ApplicationUser?.Company?.IsActive == true &&
                               (returnRequest.OrderDetail?.OrderHeader?.PaymentStatus == PaymentStatus.Deferred ||
                                returnRequest.OrderDetail?.OrderHeader?.PaymentStatus == PaymentStatus.Pending);

        if (!isCompanyDeferred)
        {
            return ReturnOperationResult.Failed("This action is only for company returns with AR adjustments.");
        }

        // Find the AR adjustment for this return
        var adjustment = await _db.Set<AccountsReceivableAdjustment>()
            .FirstOrDefaultAsync(a => a.ReturnRequestId == id);

        if (adjustment == null)
        {
            return ReturnOperationResult.Failed("AR adjustment not found for this return.");
        }

        // Apply Stripe credit balance if company has Stripe customer ID
        var company = returnRequest.OrderDetail.OrderHeader.ApplicationUser.Company;
        if (!string.IsNullOrWhiteSpace(company?.StripeCustomerId))
        {
            try
            {
                bool stripeApplied = await _arAdjustmentService.ApplyStripeCreditBalanceAsync(adjustment.Id);
                if (!stripeApplied)
                {
                    _logger.LogWarning("Failed to apply Stripe credit balance for adjustment {AdjustmentId}", adjustment.Id);
                    // Continue anyway - we still mark return as complete
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying Stripe credit balance for adjustment {AdjustmentId}", adjustment.Id);
                // Continue anyway - we still mark return as complete
            }
        }
        else
        {
            _logger.LogInformation(
                "Company {CompanyId} does not have Stripe customer ID, skipping Stripe credit balance application",
                company?.Id);

            // Mark adjustment as applied even without Stripe
            adjustment.Status = ARAdjustmentStatus.Applied;
            adjustment.AppliedAt = DateTime.UtcNow;
        }

        // Mark return as completed
        returnRequest.Status = ReturnStatus.Refunded;
        returnRequest.RefundDate = DateTime.UtcNow;
        returnRequest.RefundAmount = returnRequest.RefundAmount ?? 
                                    (returnRequest.OrderDetail.Price * returnRequest.Quantity);

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "AR adjustment return {ReturnId} finalized and marked as completed.",
            id);

        return ReturnOperationResult.Succeeded(
            "AR adjustment applied successfully. Return completed.",
            id,
            returnRequest.RefundAmount);
    }

    #endregion

    #region Helpers

    private async Task UpdateOrderStatusIfFullyRefundedAsync(int orderId)
    {
        var details = await _db.OrderDetails
            .Where(x => x.OrderHeaderId == orderId)
            .ToListAsync();

        var detailIds = details.Select(x => x.Id).ToList();

        var refunded = await _db.ReturnRequests
            .Where(r => detailIds.Contains(r.OrderDetailId)
                && r.Status == ReturnStatus.Refunded)
            .ToListAsync();

        if (refunded.Sum(x => x.Quantity) >= details.Sum(x => x.Count))
        {
            var order = await _db.OrderHeaders.FindAsync(orderId);

            if (order != null)
            {
                order.OrderStatus = OrderStatus.Refunded;
                order.PaymentStatus = PaymentStatus.Refunded;
            }
        }
    }

    #endregion

    #region Helpers Public

    public async Task<int> GetDaysRemainingInReturnWindowAsync(int orderDetailId)
    {
        var od = await _db.OrderDetails
            .Include(o => o.OrderHeader)
            .FirstOrDefaultAsync(o => o.Id == orderDetailId);

        if (od == null) return 0;

        var date = od.OrderHeader.OrderDate;

        var shipment = await _db.Shipments
            .FirstOrDefaultAsync(s =>
                s.OrderHeaderId == od.OrderHeaderId &&
                s.DeliveredDate != null);

        if (shipment?.DeliveredDate != null)
            date = shipment.DeliveredDate.Value;

        return Math.Max(0,
            SD.ReturnWindowDays - (DateTime.UtcNow - date).Days);
    }

    public List<string> GetReturnReasons()
        => SD.GetReturnReasons().ToList();

    #endregion
}