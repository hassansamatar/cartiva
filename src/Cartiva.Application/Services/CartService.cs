using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Infrastructure.Promotions;
using Cartiva.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cartiva.Application.Services;

/// <summary>
/// Service for managing shopping cart operations
/// </summary>
public class CartService : ICartService
{
    private readonly ApplicationDbContext _db;
    private readonly IPromotionService _promotionService;

    public CartService(ApplicationDbContext db, IPromotionService promotionService)
    {
        _db = db;
        _promotionService = promotionService;
    }

    public async Task<List<ShoppingCart>> GetCartItemsAsync(string userId)
    {
        return await _db.ShoppingCarts
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.Product)
                    .ThenInclude(p => p.Category)
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.SizeValue)
                    .ThenInclude(sv => sv!.SizeSystem)
            .Where(c => c.ApplicationUserId == userId)
            .ToListAsync();
    }

    public async Task<int> GetCartCountAsync(string userId)
    {
        return await _db.ShoppingCarts
            .Where(c => c.ApplicationUserId == userId)
            .SumAsync(c => c.Count);
    }

    public async Task<CartOperationResult> AddToCartAsync(string userId, int productVariantId, int count = 1)
    {
        var variant = await _db.ProductVariants
            .Include(v => v.Product)
            .Include(v => v.SizeValue)
            .FirstOrDefaultAsync(v => v.Id == productVariantId);

        if (variant == null)
            return CartOperationResult.Failed("Product variant not found.");

        var cartItem = await _db.ShoppingCarts
            .FirstOrDefaultAsync(c => c.ApplicationUserId == userId && c.ProductVariantId == productVariantId);

        int totalRequested = count + (cartItem?.Count ?? 0);

        if (totalRequested > variant.Stock)
        {
            return CartOperationResult.Failed(
                $"Cannot add {count} items. Only {variant.Stock - (cartItem?.Count ?? 0)} left in stock.");
        }

        if (cartItem != null)
        {
            cartItem.Count += count;
            cartItem.LastUpdated = DateTime.UtcNow;
        }
        else
        {
            cartItem = new ShoppingCart
            {
                ApplicationUserId = userId,
                ProductVariantId = productVariantId,
                Count = count,
                DateAdded = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };
            _db.ShoppingCarts.Add(cartItem);
        }

        await _db.SaveChangesAsync();

        var cartCount = await GetCartCountAsync(userId);
        var productInfo = GetProductInfo(variant);

        return new CartOperationResult
        {
            Success = true,
            Message = $"{productInfo} added to your cart!",
            CartCount = cartCount,
            ProductInfo = productInfo
        };
    }

    public async Task<CartOperationResult> IncrementAsync(string userId, int cartItemId)
    {
        var cartItem = await _db.ShoppingCarts
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.Product)
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.SizeValue)
            .FirstOrDefaultAsync(c => c.Id == cartItemId && c.ApplicationUserId == userId);

        if (cartItem == null)
            return CartOperationResult.Failed("Cart item not found.");

        if (cartItem.Count >= cartItem.ProductVariant.Stock)
        {
            return CartOperationResult.Failed(
                $"Cannot add more than {cartItem.ProductVariant.Stock} in stock.");
        }

        cartItem.Count++;
        cartItem.LastUpdated = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var cartCount = await GetCartCountAsync(userId);
        var productInfo = GetProductInfo(cartItem.ProductVariant);

        return new CartOperationResult
        {
            Success = true,
            Message = $"Increased quantity of {productInfo} to {cartItem.Count}.",
            CartCount = cartCount,
            NewItemCount = cartItem.Count,
            ItemSubtotal = cartItem.ProductVariant.Price * cartItem.Count,
            ProductInfo = productInfo
        };
    }

    public async Task<CartOperationResult> DecrementAsync(string userId, int cartItemId)
    {
        var cartItem = await _db.ShoppingCarts
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.Product)
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.SizeValue)
            .FirstOrDefaultAsync(c => c.Id == cartItemId && c.ApplicationUserId == userId);

        if (cartItem == null)
            return CartOperationResult.Failed("Cart item not found.");

        var productInfo = GetProductInfo(cartItem.ProductVariant);
        bool removed = false;

        cartItem.Count--;
        if (cartItem.Count <= 0)
        {
            _db.ShoppingCarts.Remove(cartItem);
            removed = true;
        }
        else
        {
            cartItem.LastUpdated = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        var cartCount = await GetCartCountAsync(userId);

        if (removed)
        {
            return new CartOperationResult
            {
                Success = true,
                Message = $"{productInfo} removed from your cart.",
                CartCount = cartCount,
                ItemRemoved = true,
                RemovedItemId = cartItemId,
                ProductInfo = productInfo
            };
        }

        return new CartOperationResult
        {
            Success = true,
            Message = $"Decreased quantity of {productInfo} to {cartItem.Count}.",
            CartCount = cartCount,
            NewItemCount = cartItem.Count,
            ItemSubtotal = cartItem.ProductVariant.Price * cartItem.Count,
            ProductInfo = productInfo
        };
    }

    public async Task<CartOperationResult> UpdateCountAsync(string userId, int cartItemId, int newCount)
    {
        var cartItem = await _db.ShoppingCarts
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.Product)
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.SizeValue)
            .FirstOrDefaultAsync(c => c.Id == cartItemId && c.ApplicationUserId == userId);

        if (cartItem == null)
            return CartOperationResult.Failed("Cart item not found.");

        var productInfo = GetProductInfo(cartItem.ProductVariant);

        if (newCount <= 0)
        {
            _db.ShoppingCarts.Remove(cartItem);
            await _db.SaveChangesAsync();
            var count = await GetCartCountAsync(userId);

            return new CartOperationResult
            {
                Success = true,
                Message = $"{productInfo} removed from your cart.",
                CartCount = count,
                ItemRemoved = true,
                RemovedItemId = cartItemId,
                ProductInfo = productInfo
            };
        }

        if (newCount > cartItem.ProductVariant.Stock)
        {
            return CartOperationResult.Failed(
                $"Cannot set quantity to {newCount}. Only {cartItem.ProductVariant.Stock} in stock.");
        }

        cartItem.Count = newCount;
        cartItem.LastUpdated = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var cartCount = await GetCartCountAsync(userId);

        return new CartOperationResult
        {
            Success = true,
            Message = $"Updated quantity of {productInfo} to {newCount}.",
            CartCount = cartCount,
            NewItemCount = newCount,
            ItemSubtotal = cartItem.ProductVariant.Price * newCount,
            ProductInfo = productInfo
        };
    }

    public async Task<CartOperationResult> RemoveFromCartAsync(string userId, int cartItemId)
    {
        var cartItem = await _db.ShoppingCarts
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.Product)
            .Include(c => c.ProductVariant)
                .ThenInclude(v => v.SizeValue)
            .FirstOrDefaultAsync(c => c.Id == cartItemId && c.ApplicationUserId == userId);

        if (cartItem == null)
            return CartOperationResult.Failed("Cart item not found.");

        var productInfo = GetProductInfo(cartItem.ProductVariant);

        _db.ShoppingCarts.Remove(cartItem);
        await _db.SaveChangesAsync();

        var cartCount = await GetCartCountAsync(userId);

        return new CartOperationResult
        {
            Success = true,
            Message = $"{productInfo} removed from your cart.",
            CartCount = cartCount,
            ItemRemoved = true,
            RemovedItemId = cartItemId,
            ProductInfo = productInfo
        };
    }

    public async Task ClearCartAsync(string userId)
    {
        var cartItems = await _db.ShoppingCarts
            .Where(c => c.ApplicationUserId == userId)
            .ToListAsync();

        _db.ShoppingCarts.RemoveRange(cartItems);
        await _db.SaveChangesAsync();
    }

    public async Task<CartTotals> CalculateTotalsAsync(string userId)
    {
        var cartItems = await GetCartItemsAsync(userId);

        if (!cartItems.Any())
        {
            return new CartTotals();
        }

        var discount = await _promotionService.CalculateDiscountAsync(cartItems);

        // Calculate VAT-aware totals
        var subtotalIncVat = cartItems.Sum(c => c.ProductVariant.PriceIncVat * c.Count);
        var subtotalExVat = cartItems.Sum(c => c.ProductVariant.PriceExVat * c.Count);

        // Fallback for legacy data where PriceExVat is 0
        if (subtotalExVat == 0)
        {
            subtotalIncVat = cartItems.Sum(c => c.ProductVariant.Price * c.Count);
            subtotalExVat = subtotalIncVat / 1.25m;
        }

        var totalVat = subtotalIncVat - subtotalExVat;

        return new CartTotals
        {
            SubtotalIncVat = subtotalIncVat,
            SubtotalExVat = subtotalExVat,
            TotalVat = totalVat,
            TotalDiscount = discount.TotalDiscount,
            FinalTotal = subtotalIncVat - discount.TotalDiscount,
            AppliedPromotions = discount.AppliedPromotions.Select(p => new AppliedPromotionInfo
            {
                DisplayText = p.DisplayText,
                CategoryName = p.CategoryName,
                Discount = p.Discount,
                FreeItemCount = p.FreeItemCount
            }).ToList()
        };
    }

    public async Task<List<StockValidationResult>> ValidateStockAsync(string userId)
    {
        var cartItems = await GetCartItemsAsync(userId);
        var insufficientStock = new List<StockValidationResult>();

        foreach (var cart in cartItems)
        {
            var variant = cart.ProductVariant;
            if (variant.Stock < cart.Count)
            {
                insufficientStock.Add(new StockValidationResult
                {
                    ProductVariantId = variant.Id,
                    ProductName = variant.Product?.Name ?? "Unknown",
                    Color = variant.Color,
                    Size = variant.SizeValue?.DisplayText,
                    RequestedQuantity = cart.Count,
                    AvailableStock = variant.Stock
                });
            }
        }

        return insufficientStock;
    }

    private static string GetProductInfo(ProductVariant variant)
    {
        var sizeDisplay = variant.SizeValue?.DisplayText ?? "No Size";
        return $"{variant.Product?.Name} ({variant.Color}/{sizeDisplay})";
    }
}
