using Cartiva.Domain;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Cartiva.Application.Abstractions;

/// <summary>
/// Service for managing category operations
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Get all categories with size systems
    /// </summary>
    Task<List<Category>> GetAllCategoriesAsync();

    /// <summary>
    /// Get a category by ID
    /// </summary>
    Task<Category?> GetCategoryByIdAsync(int id);

    /// <summary>
    /// Create a new category
    /// </summary>
    Task<CategoryOperationResult> CreateCategoryAsync(Category category);

    /// <summary>
    /// Update an existing category
    /// </summary>
    Task<CategoryOperationResult> UpdateCategoryAsync(Category category);

    /// <summary>
    /// Delete a category (only if no products assigned)
    /// </summary>
    Task<CategoryOperationResult> DeleteCategoryAsync(int id);

    /// <summary>
    /// Check if category name already exists
    /// </summary>
    Task<bool> CategoryNameExistsAsync(string name, int? excludeId = null);

    /// <summary>
    /// Get product count for a category
    /// </summary>
    Task<int> GetProductCountAsync(int categoryId);

    /// <summary>
    /// Get variant count for a category
    /// </summary>
    Task<int> GetVariantCountAsync(int categoryId);

    /// <summary>
    /// Check if category has products
    /// </summary>
    Task<bool> HasProductsAsync(int categoryId);

    /// <summary>
    /// Get size system select list for dropdowns
    /// </summary>
    Task<List<SelectListItem>> GetSizeSystemSelectListAsync(int? selectedId = null);

    /// <summary>
    /// Get products for a category (limited for display)
    /// </summary>
    Task<List<CategoryProductInfo>> GetCategoryProductsAsync(int categoryId, int take = 5);
}

/// <summary>
/// Result of a category operation
/// </summary>
public class CategoryOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public Dictionary<string, string> ValidationErrors { get; set; } = new();

    public static CategoryOperationResult Succeeded(string message, int? entityId = null)
        => new() { Success = true, Message = message, EntityId = entityId };

    public static CategoryOperationResult Failed(string message)
        => new() { Success = false, Message = message };

    public static CategoryOperationResult ValidationFailed(string field, string message)
        => new() { Success = false, ValidationErrors = new Dictionary<string, string> { { field, message } } };
}

/// <summary>
/// Basic product info for category display
/// </summary>
public class CategoryProductInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
