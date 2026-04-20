using Cartiva.Domain;
using Cartiva.Domain.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cartiva.Application.Abstractions;

/// <summary>
/// Service for managing products and product variants
/// </summary>
public interface IProductService
{
    #region Products

    /// <summary>
    /// Get all products with categories and variants
    /// </summary>
    Task<List<Product>> GetAllProductsAsync();

    /// <summary>
    /// Get a product by ID with all related data
    /// </summary>
    Task<Product?> GetProductByIdAsync(int id);

    /// <summary>
    /// Create a new product
    /// </summary>
    Task<ProductOperationResult> CreateProductAsync(Product product, IFormFile? imageFile);

    /// <summary>
    /// Update an existing product
    /// </summary>
    Task<ProductOperationResult> UpdateProductAsync(Product product, IFormFile? imageFile);

    /// <summary>
    /// Delete a product (only if it has no variants)
    /// </summary>
    Task<ProductOperationResult> DeleteProductAsync(int id);

    /// <summary>
    /// Get category select list for dropdowns
    /// </summary>
    Task<List<SelectListItem>> GetCategorySelectListAsync();

    /// <summary>
    /// Get size system info for a category
    /// </summary>
    Task<CategorySizeSystemInfo?> GetCategorySizeSystemAsync(int categoryId);

    #endregion

    #region Variants

    /// <summary>
    /// Get all variants for a product
    /// </summary>
    Task<List<ProductVariant>> GetVariantsByProductIdAsync(int productId);

    /// <summary>
    /// Get a variant by ID with all related data
    /// </summary>
    Task<ProductVariant?> GetVariantByIdAsync(int id);

    /// <summary>
    /// Create a new product variant
    /// </summary>
    Task<ProductOperationResult> CreateVariantAsync(ProductVariant variant);

    /// <summary>
    /// Update an existing product variant
    /// </summary>
    Task<ProductOperationResult> UpdateVariantAsync(ProductVariant variant);

    /// <summary>
    /// Delete a product variant
    /// </summary>
    Task<ProductOperationResult> DeleteVariantAsync(int id);

    /// <summary>
    /// Get available sizes for a product's category
    /// </summary>
    Task<List<SelectListItem>> GetAvailableSizesAsync(int productId);

    /// <summary>
    /// Get list of available colors
    /// </summary>
    List<SelectListItem> GetColorSelectList(string? selectedColor = null);

    /// <summary>
    /// Validate variant (color, size, duplicates)
    /// </summary>
    Task<VariantValidationResult> ValidateVariantAsync(ProductVariant variant, bool isUpdate = false);

    #endregion
}

/// <summary>
/// Result of a product/variant operation
/// </summary>
public class ProductOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public Dictionary<string, string> ValidationErrors { get; set; } = new();

    public static ProductOperationResult Succeeded(string message, int? entityId = null)
        => new() { Success = true, Message = message, EntityId = entityId };

    public static ProductOperationResult Failed(string message)
        => new() { Success = false, Message = message };

    public static ProductOperationResult ValidationFailed(Dictionary<string, string> errors)
        => new() { Success = false, Message = "Validation failed", ValidationErrors = errors };
}

/// <summary>
/// Information about a category's size system
/// </summary>
public class CategorySizeSystemInfo
{
    public bool HasSizeSystem { get; set; }
    public string? SizeSystemName { get; set; }
    public int? SizeSystemId { get; set; }
    public string? IconClass { get; set; }
    public string? AlertClass { get; set; }
}

/// <summary>
/// Result of variant validation
/// </summary>
public class VariantValidationResult
{
    public bool IsValid { get; set; }
    public Dictionary<string, string> Errors { get; set; } = new();

    public static VariantValidationResult Valid() => new() { IsValid = true };

    public static VariantValidationResult Invalid(string field, string message)
        => new() { IsValid = false, Errors = new Dictionary<string, string> { { field, message } } };
}
