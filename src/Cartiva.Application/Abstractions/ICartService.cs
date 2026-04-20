using Cartiva.Domain;

namespace Cartiva.Application.Abstractions;

/// <summary>
/// Service for managing shopping cart operations
/// </summary>
public interface ICartService
{
    /// <summary>
    /// Get all cart items for a user with full product details
    /// </summary>
    Task<List<ShoppingCart>> GetCartItemsAsync(string userId);

    /// <summary>
    /// Get the total count of items in the cart
    /// </summary>
    Task<int> GetCartCountAsync(string userId);

    /// <summary>
    /// Add an item to the cart or increment if already exists
    /// </summary>
    /// <returns>Result with success status, message, and updated cart count</returns>
    Task<CartOperationResult> AddToCartAsync(string userId, int productVariantId, int count = 1);

    /// <summary>
    /// Increment the quantity of a cart item
    /// </summary>
    Task<CartOperationResult> IncrementAsync(string userId, int cartItemId);

    /// <summary>
    /// Decrement the quantity of a cart item (removes if count reaches 0)
    /// </summary>
    Task<CartOperationResult> DecrementAsync(string userId, int cartItemId);

    /// <summary>
    /// Update the count of a cart item directly
    /// </summary>
    Task<CartOperationResult> UpdateCountAsync(string userId, int cartItemId, int newCount);

    /// <summary>
    /// Remove an item from the cart
    /// </summary>
    Task<CartOperationResult> RemoveFromCartAsync(string userId, int cartItemId);

    /// <summary>
    /// Clear all items from a user's cart
    /// </summary>
    Task ClearCartAsync(string userId);

    /// <summary>
    /// Calculate cart totals including VAT and promotions
    /// </summary>
    Task<CartTotals> CalculateTotalsAsync(string userId);

    /// <summary>
    /// Validate stock availability for all cart items
    /// </summary>
    /// <returns>List of items with insufficient stock</returns>
    Task<List<StockValidationResult>> ValidateStockAsync(string userId);
}

/// <summary>
/// Result of a cart operation
/// </summary>
public class CartOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CartCount { get; set; }
    public int? NewItemCount { get; set; }
    public decimal? ItemSubtotal { get; set; }
    public bool ItemRemoved { get; set; }
    public int? RemovedItemId { get; set; }
    public string? ProductInfo { get; set; }

    public static CartOperationResult Succeeded(string message, int cartCount, string? productInfo = null)
        => new() { Success = true, Message = message, CartCount = cartCount, ProductInfo = productInfo };

    public static CartOperationResult Failed(string message)
        => new() { Success = false, Message = message };
}

/// <summary>
/// Cart totals with VAT breakdown
/// </summary>
public class CartTotals
{
    public decimal SubtotalIncVat { get; set; }
    public decimal SubtotalExVat { get; set; }
    public decimal TotalVat { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal FinalTotal { get; set; }
    public List<AppliedPromotionInfo> AppliedPromotions { get; set; } = new();
}

/// <summary>
/// Information about an applied promotion
/// </summary>
public class AppliedPromotionInfo
{
    public string DisplayText { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal Discount { get; set; }
    public int FreeItemCount { get; set; }
}

/// <summary>
/// Result of stock validation
/// </summary>
public class StockValidationResult
{
    public int ProductVariantId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public string? Size { get; set; }
    public int RequestedQuantity { get; set; }
    public int AvailableStock { get; set; }
}
