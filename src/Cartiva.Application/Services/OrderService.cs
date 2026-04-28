using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Domain.Extensions;
using Cartiva.Domain.Enums;
using Cartiva.Domain.Interfaces;
using Cartiva.Domain.ViewModels;
using Cartiva.Infrastructure.Promotions;
using Cartiva.Persistence;
using Cartiva.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;
using DomainCreditNote = Cartiva.Domain.CreditNote;

namespace Cartiva.Application.Services;

/// <summary>
/// Service for managing order operations
/// </summary>
public class OrderService : IOrderService
{
    private readonly ApplicationDbContext _db;
    private readonly IPromotionService _promotionService;
    private readonly IInvoiceService _invoiceService;
    private readonly INotificationService _notificationService;
    private readonly ICreditNoteService _creditNoteService;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        ApplicationDbContext db,
        IPromotionService promotionService,
        IInvoiceService invoiceService,
        INotificationService notificationService,
        ICreditNoteService creditNoteService,
        ILogger<OrderService> logger)
    {
        _db = db;
        _promotionService = promotionService;
        _invoiceService = invoiceService;
        _notificationService = notificationService;
        _creditNoteService = creditNoteService;
        _logger = logger;
    }

    #region Order Retrieval

    public async Task<OrderHeader?> GetOrderByIdAsync(int orderId)
    {
        return await _db.OrderHeaders
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(v => v.Product)
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(v => v.SizeValue)
            .Include(o => o.ApplicationUser)
            .Include(o => o.Shipments)
            .FirstOrDefaultAsync(o => o.Id == orderId);
    }

    public async Task<List<OrderHeader>> GetOrdersByUserIdAsync(string userId)
    {
        return await _db.OrderHeaders
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
                    .ThenInclude(v => v.Product)
            .Where(o => o.ApplicationUserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<List<OrderHeader>> GetAllOrdersAsync(string? statusFilter = null)
    {
        var query = _db.OrderHeaders
            .Include(o => o.OrderDetails)
            .Include(o => o.ApplicationUser)
            .Include(o => o.Shipments)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter))
        {
            var parsedStatus = OrderStatusExtensions.FromValue(statusFilter);
            query = query.Where(o => o.OrderStatus == parsedStatus);
        }

        return await query.OrderByDescending(o => o.OrderDate).ToListAsync();
    }

    #endregion

    #region Checkout & Order Creation

    public async Task<CheckoutResult> PrepareCheckoutAsync(string userId)
    {
        var cartList = await _db.ShoppingCarts
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.Product)
                    .ThenInclude(p => p.Category)
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.SizeValue)
                    .ThenInclude(sv => sv!.SizeSystem)
            .Where(c => c.ApplicationUserId == userId)
            .ToListAsync();

        if (!cartList.Any())
        {
            return CheckoutResult.Empty("Your cart is empty.");
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var discount = await _promotionService.CalculateDiscountAsync(cartList);

        // Calculate VAT-aware totals
        var subtotalIncVat = cartList.Sum(c => c.ProductVariant.PriceIncVat * c.Count);
        var subtotalExVat = cartList.Sum(c => c.ProductVariant.PriceExVat * c.Count);

        // Fallback for legacy data where PriceExVat is 0
        if (subtotalExVat == 0)
        {
            subtotalIncVat = cartList.Sum(c => c.ProductVariant.Price * c.Count);
            subtotalExVat = subtotalIncVat / 1.25m;
        }

        var totalVat = subtotalIncVat - subtotalExVat;

        return new CheckoutResult
        {
            Success = true,
            CartItems = cartList,
            OrderHeader = new OrderHeader
            {
                Name = user?.Name ?? string.Empty,
                PhoneNumber = user?.PhoneNumber,
                StreetAddress = user?.StreetAddress,
                City = user?.City,
                State = user?.State ?? user?.City,
                PostalCode = user?.PostalCode,
                Country = user?.Country ?? "Norway"
            },
            Subtotal = subtotalIncVat,
            SubtotalExVat = subtotalExVat,
            TotalVat = totalVat,
            TotalDiscount = discount.TotalDiscount,
            OrderTotal = subtotalIncVat - discount.TotalDiscount,
            AppliedPromotions = discount.AppliedPromotions.Select(p => new AppliedPromotionInfo
            {
                DisplayText = p.DisplayText,
                CategoryName = p.CategoryName,
                Discount = p.Discount,
                FreeItemCount = p.FreeItemCount
            }).ToList()
        };
    }

    public async Task<PlaceOrderResult> PlaceOrderAsync(string userId, OrderHeader orderHeader, bool payNow = false)
    {
        var cartList = await _db.ShoppingCarts
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.Product)
                    .ThenInclude(p => p.Category)
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.SizeValue)
            .Where(c => c.ApplicationUserId == userId)
            .ToListAsync();

        if (!cartList.Any())
        {
            return PlaceOrderResult.Failed("Your cart is empty.");
        }

        // Validate stock
        foreach (var cart in cartList)
        {
            var variant = cart.ProductVariant;
            if (variant.Stock < cart.Count)
            {
                string sizeDisplay = variant.SizeValue?.DisplayText ?? "No Size";
                return PlaceOrderResult.Failed(
                    $"Not enough stock for {variant.Product?.Name} ({variant.Color}/{sizeDisplay}). Only {variant.Stock} left.");
            }
        }

        var user = await _db.Users.FindAsync(userId);
        var companyStatus = await CheckCompanyStatusAsync(userId);

        // Calculate promotion discount
        var discount = await _promotionService.CalculateDiscountAsync(cartList);

        // Calculate VAT-aware totals
        var subtotalIncVat = cartList.Sum(c => c.ProductVariant.PriceIncVat * c.Count);
        var subtotalExVat = cartList.Sum(c => c.ProductVariant.PriceExVat * c.Count);
        var totalVat = subtotalIncVat - subtotalExVat;

        // Fallback for legacy data
        if (subtotalExVat == 0)
        {
            subtotalIncVat = cartList.Sum(c => c.ProductVariant.Price * c.Count);
            subtotalExVat = subtotalIncVat / 1.25m;
            totalVat = subtotalIncVat - subtotalExVat;
        }

        // Set order header fields
        orderHeader.ApplicationUserId = userId;
        orderHeader.OrderDate = DateTime.Now;
        orderHeader.Currency = SD.DefaultCurrency;
        orderHeader.SubtotalExVat = subtotalExVat - (discount.TotalDiscount / 1.25m);
        orderHeader.TotalVatAmount = (subtotalExVat - (discount.TotalDiscount / 1.25m)) * 0.25m;
        orderHeader.TotalDiscountAmount = discount.TotalDiscount;
        orderHeader.OrderTotal = subtotalIncVat - discount.TotalDiscount;
        orderHeader.Country = user?.Country ?? "Norway";

        bool isDeferredPayment = false;
        bool requiresPayment = true;

        // Determine payment logic based on company status
        if (companyStatus.IsCompanyUser)
        {
            if (companyStatus.IsCompanyActive)
            {
                // Active company – allow deferred payment
                orderHeader.PaymentStatus = Cartiva.Domain.Enums.PaymentStatus.Deferred;
                orderHeader.OrderStatus = Cartiva.Domain.Enums.OrderStatus.AwaitingShipmentApproval;
                orderHeader.PaymentDueDate = DateOnly.FromDateTime(orderHeader.OrderDate.AddDays(30));
                orderHeader.ReturnExpirationDate = DateTime.Now.AddDays(14);
                isDeferredPayment = true;
                requiresPayment = payNow; // Only requires payment if explicitly requested
            }
            else
            {
                // Inactive company – force upfront payment
                orderHeader.PaymentStatus = Cartiva.Domain.Enums.PaymentStatus.Pending;
                orderHeader.OrderStatus = Cartiva.Domain.Enums.OrderStatus.Pending;
                orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now);
                orderHeader.ReturnExpirationDate = DateTime.Now.AddDays(30);
            }
        }
        else
        {
            // Regular customer – payment required
            orderHeader.PaymentStatus = Cartiva.Domain.Enums.PaymentStatus.Pending;
            orderHeader.OrderStatus = Cartiva.Domain.Enums.OrderStatus.Pending;
            orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now);
            orderHeader.ReturnExpirationDate = DateTime.Now.AddDays(30);
        }

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            _db.OrderHeaders.Add(orderHeader);
            await _db.SaveChangesAsync();

            // Create OrderDetails with full VAT breakdown and update stock
            var orderDetails = new List<OrderDetail>();
            foreach (var cart in cartList)
            {
                var orderDetail = OrderDetail.FromProductVariant(cart.ProductVariant, cart.Count);
                orderDetail.OrderHeaderId = orderHeader.Id;
                orderDetails.Add(orderDetail);

                // Update stock
                cart.ProductVariant.Stock -= cart.Count;
            }

            _db.OrderDetails.AddRange(orderDetails);

            // Clear cart
            _db.ShoppingCarts.RemoveRange(cartList);
            await _db.SaveChangesAsync();

            // Update OrderHeader totals with VAT breakdown
            orderHeader.OrderDetails = orderDetails;
            orderHeader.RecalculateTotals();
            await _db.SaveChangesAsync();

            await _invoiceService.GenerateInvoiceFromOrderAsync(orderHeader.Id);

            await transaction.CommitAsync();

            _logger.LogInformation("Order {OrderId} placed successfully for user {UserId}", orderHeader.Id, userId);

            // Send order confirmation notification
            var orderConfirmationRequest = new NotificationRequest(
                Recipient: user.Email,
                Type: NotificationType.OrderConfirmation,
                TemplateData: new Dictionary<string, object>
                {
                    ["orderId"] = orderHeader.Id.ToString(),
                    ["name"] = string.IsNullOrWhiteSpace(user.Name) ? orderHeader.Name : user.Name,
                    ["orderDate"] = orderHeader.OrderDate.ToString("yyyy-MM-dd"),
                    ["totalAmount"] = $"{orderHeader.OrderTotal:N2} {SD.DefaultCurrency}",
                    ["currency"] = SD.DefaultCurrency
                },
                UserId: userId,
                ReferenceId: orderHeader.Id.ToString(),
                ReferenceType: "Order",
                Subject: $"Order Confirmation - Order #{orderHeader.Id}"
            );

            await _notificationService.SendAsync(orderConfirmationRequest);

            return PlaceOrderResult.Succeeded(
                orderHeader.Id,
                requiresPayment,
                companyStatus.IsCompanyUser,
                isDeferredPayment);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error placing order for user {UserId}", userId);
            return PlaceOrderResult.Failed("An error occurred while placing your order. Please try again.");
        }
    }

    public async Task<CompanyStatusResult> CheckCompanyStatusAsync(string userId)
    {
        var user = await _db.Users.FindAsync(userId);

        if (user?.CompanyId == null)
        {
            return new CompanyStatusResult { IsCompanyUser = false };
        }

        var company = await _db.Companies.FindAsync(user.CompanyId);

        return new CompanyStatusResult
        {
            IsCompanyUser = true,
            IsCompanyActive = company?.IsActive ?? false,
            CompanyId = company?.Id,
            CompanyName = company?.Name
        };
    }

    #endregion

    #region Order Status Management

    public async Task<OrderOperationResult> UpdateOrderStatusAsync(int orderId, string newStatus)
    {
        var order = await _db.OrderHeaders.FindAsync(orderId);
        if (order == null)
            return OrderOperationResult.Failed("Order not found.");

        order.OrderStatus = Cartiva.Domain.Extensions.OrderStatusExtensions.FromValue(newStatus);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, newStatus);
        return OrderOperationResult.Succeeded($"Order status updated to {newStatus}.");
    }

    public async Task<OrderOperationResult> UpdatePaymentStatusAsync(int orderId, string paymentStatus, string? paymentIntentId = null)
    {
        var order = await _db.OrderHeaders.FindAsync(orderId);
        if (order == null)
            return OrderOperationResult.Failed("Order not found.");

        order.PaymentStatus = Cartiva.Domain.Extensions.PaymentStatusExtensions.FromValue(paymentStatus);
        if (paymentIntentId != null)
        {
            order.PaymentIntentId = paymentIntentId;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} payment status updated to {Status}", orderId, paymentStatus);
        return OrderOperationResult.Succeeded($"Payment status updated to {paymentStatus}.");
    }

    public async Task<OrderOperationResult> ProcessPaymentSuccessAsync(int orderId, string paymentIntentId)
    {
        var order = await _db.OrderHeaders
            .Include(o => o.ApplicationUser)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return OrderOperationResult.Failed("Order not found.");

        order.PaymentStatus = Cartiva.Domain.Enums.PaymentStatus.Approved;
        order.PaymentIntentId = paymentIntentId;
        order.PaymentDate = DateTime.Now;
        order.OrderStatus = Cartiva.Domain.Enums.OrderStatus.AwaitingShipmentApproval;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Payment processed successfully for order {OrderId}", orderId);

        // Send payment received notification
        if (order.ApplicationUser?.Email != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.SendAsync(new NotificationRequest(
                        Recipient: order.ApplicationUser.Email,
                        Type: NotificationType.PaymentReceived,
                        TemplateData: new Dictionary<string, object>
                        {
                            ["paymentId"] = "0",
                            ["invoiceId"] = "0",
                            ["orderId"] = orderId.ToString(),
                            ["amount"] = order.OrderTotal.ToString(System.Globalization.CultureInfo.InvariantCulture),
                            ["paymentDate"] = order.PaymentDate?.ToString("yyyy-MM-ddTHH:mm:ss") ?? DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                            ["paymentReference"] = order.PaymentIntentId ?? string.Empty,
                            ["paymentMethod"] = "Card",
                            ["transactionId"] = paymentIntentId,
                            ["invoiceNumber"] = string.Empty,
                            ["customerName"] = string.IsNullOrWhiteSpace(order.ApplicationUser?.Name) ? order.Name : order.ApplicationUser.Name,
                            ["currency"] = order.Currency
                        },
                        UserId: order.ApplicationUserId,
                        ReferenceId: orderId.ToString(),
                        ReferenceType: "Order",
                        Subject: $"Payment Received - Order #{orderId}"
                    ));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send payment received notification for order {OrderId}", orderId);
                }
            });
        }

        return OrderOperationResult.Succeeded("Payment processed successfully.");
    }

    public async Task<OrderOperationResult> CancelOrderAsync(int orderId, string? reason = null)
    {
        var order = await _db.OrderHeaders
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.ProductVariant)
            .Include(o => o.ApplicationUser)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return OrderOperationResult.Failed("Order not found.");

        if (order.OrderStatus == Cartiva.Domain.Enums.OrderStatus.Cancelled)
            return OrderOperationResult.Failed("Order is already cancelled.");

        var cancellationReason = string.IsNullOrWhiteSpace(reason) ? "Not specified" : reason;

        DomainCreditNote? creditNote = null;
        string? refundReference = null;

        if (IsPaidOrder(order))
        {
            try
            {
                creditNote = await _creditNoteService.CreateFromCancelledOrderAsync(
                    orderId,
                    $"Order cancellation - {cancellationReason}",
                    order.ApplicationUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Credit note creation failed for cancelled order {OrderId}", orderId);
                return OrderOperationResult.Failed("Order cancellation failed because the credit note could not be created.");
            }

            if (!string.IsNullOrWhiteSpace(order.PaymentIntentId))
            {
                try
                {
                    var refundService = new RefundService();
                    var refundAmount = creditNote?.TotalAmount ?? order.OrderTotal;

                    var refund = await refundService.CreateAsync(new RefundCreateOptions
                    {
                        PaymentIntent = order.PaymentIntentId,
                        Amount = (long)Math.Round(refundAmount * 100, MidpointRounding.AwayFromZero)
                    });

                    if (refund.Status != "succeeded" && refund.Status != "pending")
                    {
                        return OrderOperationResult.Failed($"Refund failed with status: {refund.Status}.");
                    }

                    refundReference = refund.Id;
                }
                catch (StripeException ex) when (ex.Message != null && ex.Message.Contains("already been refunded", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Stripe payment already refunded for order {OrderId}", orderId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Refund processing failed for cancelled order {OrderId}", orderId);
                    return OrderOperationResult.Failed("Order cancellation failed because the refund could not be processed automatically.");
                }
            }

            order.PaymentStatus = Cartiva.Domain.Enums.PaymentStatus.Refunded;
            order.PaymentDate = DateTime.UtcNow;

            if (creditNote != null && !string.IsNullOrWhiteSpace(refundReference))
            {
                creditNote.ExternalRefundReference = refundReference;
            }
        }

        // Restore stock
        foreach (var detail in order.OrderDetails)
        {
            if (detail.ProductVariant != null)
            {
                detail.ProductVariant.Stock += detail.Count;
            }
        }

        order.OrderStatus = Cartiva.Domain.Enums.OrderStatus.Cancelled;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Order {OrderId} cancelled. Reason: {Reason}", orderId, cancellationReason);

        // Send order cancelled notification
        if (order.ApplicationUser?.Email != null)
        {
            try
            {
                await _notificationService.SendAsync(new NotificationRequest(
                    Recipient: order.ApplicationUser.Email,
                    Type: NotificationType.OrderCancelled,
                    TemplateData: new Dictionary<string, object>
                    {
                        ["orderId"] = orderId.ToString(),
                        ["name"] = string.IsNullOrWhiteSpace(order.ApplicationUser?.Name) ? order.Name : order.ApplicationUser.Name,
                        ["orderDate"] = order.OrderDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        ["orderTotal"] = order.OrderTotal.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        ["currency"] = order.Currency,
                        ["orderStatus"] = order.OrderStatus?.ToString() ?? string.Empty,
                        ["paymentStatus"] = order.PaymentStatus?.ToString() ?? string.Empty,
                        ["cancellationReason"] = cancellationReason
                    },
                    UserId: order.ApplicationUserId,
                    ReferenceId: orderId.ToString(),
                    ReferenceType: "Order",
                    Subject: $"Order Cancelled - Order #{orderId}"
                ));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send order cancelled notification for order {OrderId}", orderId);
            }
        }

        return OrderOperationResult.Succeeded("Order cancelled successfully.");
    }

    private static bool IsPaidOrder(OrderHeader order)
    {
        if (!string.IsNullOrWhiteSpace(order.PaymentIntentId))
        {
            return true;
        }

        return order.PaymentStatus == Cartiva.Domain.Enums.PaymentStatus.Approved ||
               order.PaymentStatus == Cartiva.Domain.Enums.PaymentStatus.Paid;
    }

    #endregion

    #region Order Calculations

    public async Task RecalculateOrderTotalsAsync(int orderId)
    {
        var order = await _db.OrderHeaders
            .Include(o => o.OrderDetails)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order != null)
        {
            order.RecalculateTotals();
            await _db.SaveChangesAsync();
        }
    }

    #endregion
}
