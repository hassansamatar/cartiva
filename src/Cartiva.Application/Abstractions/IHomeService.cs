using Cartiva.Domain;

namespace Cartiva.Application.Abstractions;

/// <summary>
/// Service for customer-facing home/browsing operations
/// </summary>
public interface IHomeService
{
    /// <summary>
    /// Get all products for browsing with variants and reviews
    /// </summary>
    Task<List<Product>> GetAllProductsForBrowsingAsync();

    /// <summary>
    /// Get product details by ID with full variant and review information
    /// </summary>
    Task<Product?> GetProductDetailsAsync(int productId);

    /// <summary>
    /// Get active promotions
    /// </summary>
    Task<List<Promotion>> GetActivePromotionsAsync();

    /// <summary>
    /// Search products by name or description
    /// </summary>
    Task<List<Product>> SearchProductsAsync(string searchTerm);

    /// <summary>
    /// Get products by category
    /// </summary>
    Task<List<Product>> GetProductsByCategoryAsync(int categoryId);

    /// <summary>
    /// Get featured products (e.g., most reviewed, best rated)
    /// </summary>
    Task<List<Product>> GetFeaturedProductsAsync(int count = 8);

    /// <summary>
    /// Get all categories for filtering
    /// </summary>
    Task<List<Category>> GetCategoriesAsync();
}
