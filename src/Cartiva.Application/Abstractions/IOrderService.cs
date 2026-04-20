using Cartiva.Domain;
using Cartiva.Domain.ViewModels;

namespace Cartiva.Application.Abstractions;

/// <summary>
/// Service for managing order operations
/// </summary>
public interface IOrderService
{
    #region Order Retrieval

    /// <summary>
    /// Get an order by ID with full details
    /// </summary>
    Task<OrderHeader?> GetOrderByIdAsync(int orderId);

    /// <summary>
    /// Get orders for a specific user
    /// </summary>
    Task<List<OrderHeader>> GetOrdersByUserIdAsync(string userId);

    /// <summary>
    /// Get all orders with optional status filter
    /// </summary>
    Task<List<OrderHeader>> GetAllOrdersAsync(string? statusFilter = null);

    #endregion

    #region Checkout & Order Creation

    /// <summary>
    /// Prepare checkout data for a user
    /// </summary>
    Task<CheckoutResult> PrepareCheckoutAsync(string userId);

    /// <summary>
    /// Place an order from the user's cart
    /// </summary>
    Task<PlaceOrderResult> PlaceOrderAsync(string userId, OrderHeader orderHeader, bool payNow = false);

    /// <summary>
    /// Check if user's company is active (for deferred payment eligibility)
    /// </summary>
    Task<CompanyStatusResult> CheckCompanyStatusAsync(string userId);

    #endregion

    #region Order Status Management

    /// <summary>
    /// Update order status
    /// </summary>
    Task<OrderOperationResult> UpdateOrderStatusAsync(int orderId, string newStatus);

    /// <summary>
    /// Update payment status
    /// </summary>
    Task<OrderOperationResult> UpdatePaymentStatusAsync(int orderId, string paymentStatus, string? paymentIntentId = null);

    /// <summary>
    /// Process successful payment
    /// </summary>
    Task<OrderOperationResult> ProcessPaymentSuccessAsync(int orderId, string paymentIntentId);

    /// <summary>
    /// Cancel an order
    /// </summary>
    Task<OrderOperationResult> CancelOrderAsync(int orderId, string? reason = null);

    #endregion

    #region Order Calculations

    /// <summary>
    /// Recalculate order totals
    /// </summary>
    Task RecalculateOrderTotalsAsync(int orderId);

    #endregion
}

/// <summary>
/// Result of checkout preparation
/// </summary>
public class CheckoutResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ShoppingCart> CartItems { get; set; } = new();
    public OrderHeader? OrderHeader { get; set; }
    public decimal Subtotal { get; set; }
    public decimal SubtotalExVat { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal OrderTotal { get; set; }
    public List<AppliedPromotionInfo> AppliedPromotions { get; set; } = new();

    public static CheckoutResult Empty(string message)
        => new() { Success = false, ErrorMessage = message };
}

/// <summary>
/// Result of placing an order
/// </summary>
public class PlaceOrderResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? OrderId { get; set; }
    public bool RequiresPayment { get; set; }
    public bool IsCompanyOrder { get; set; }
    public bool IsDeferredPayment { get; set; }

    public static PlaceOrderResult Failed(string message)
        => new() { Success = false, ErrorMessage = message };

    public static PlaceOrderResult Succeeded(int orderId, bool requiresPayment, bool isCompanyOrder, bool isDeferredPayment)
        => new()
        {
            Success = true,
            OrderId = orderId,
            RequiresPayment = requiresPayment,
            IsCompanyOrder = isCompanyOrder,
            IsDeferredPayment = isDeferredPayment
        };
}

/// <summary>
/// Result of company status check
/// </summary>
public class CompanyStatusResult
{
    public bool IsCompanyUser { get; set; }
    public bool IsCompanyActive { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
}

/// <summary>
/// Result of an order operation
/// </summary>
public class OrderOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;

    public static OrderOperationResult Succeeded(string message)
        => new() { Success = true, Message = message };

    public static OrderOperationResult Failed(string message)
        => new() { Success = false, Message = message };
}
