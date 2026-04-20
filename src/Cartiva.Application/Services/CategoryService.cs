using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Persistence;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cartiva.Application.Services;

/// <summary>
/// Service for managing category operations
/// </summary>
public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(ApplicationDbContext db, ILogger<CategoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Category>> GetAllCategoriesAsync()
    {
        return await _db.Categories
            .Include(c => c.DefaultSizeSystem)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetCategoryByIdAsync(int id)
    {
        return await _db.Categories
            .Include(c => c.DefaultSizeSystem)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<CategoryOperationResult> CreateCategoryAsync(Category category)
    {
        if (await CategoryNameExistsAsync(category.Name))
        {
            return CategoryOperationResult.ValidationFailed("Name", "A category with this name already exists.");
        }

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Category created: {CategoryName} (ID: {CategoryId})", category.Name, category.Id);
        return CategoryOperationResult.Succeeded($"Category '{category.Name}' created successfully", category.Id);
    }

    public async Task<CategoryOperationResult> UpdateCategoryAsync(Category category)
    {
        if (await CategoryNameExistsAsync(category.Name, category.Id))
        {
            return CategoryOperationResult.ValidationFailed("Name", "A category with this name already exists.");
        }

        try
        {
            _db.Categories.Update(category);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Category updated: {CategoryName} (ID: {CategoryId})", category.Name, category.Id);
            return CategoryOperationResult.Succeeded($"Category '{category.Name}' updated successfully", category.Id);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "Concurrency error updating category ID: {CategoryId}", category.Id);
            return CategoryOperationResult.Failed("The category was modified by another user. Please try again.");
        }
    }

    public async Task<CategoryOperationResult> DeleteCategoryAsync(int id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category == null)
        {
            return CategoryOperationResult.Failed("Category not found.");
        }

        if (await HasProductsAsync(id))
        {
            int productCount = await GetProductCountAsync(id);
            return CategoryOperationResult.Failed(
                $"Cannot delete category '{category.Name}' because it has {productCount} product(s) assigned to it.");
        }

        string categoryName = category.Name;

        try
        {
            _db.Categories.Remove(category);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Category deleted: {CategoryName} (ID: {CategoryId})", categoryName, id);
            return CategoryOperationResult.Succeeded($"Category '{categoryName}' deleted successfully");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error deleting category ID: {CategoryId}", id);
            return CategoryOperationResult.Failed("Cannot delete this category because it is referenced by other records.");
        }
    }

    public async Task<bool> CategoryNameExistsAsync(string name, int? excludeId = null)
    {
        var query = _db.Categories.Where(c => c.Name.ToLower() == name.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<int> GetProductCountAsync(int categoryId)
    {
        return await _db.Products.CountAsync(p => p.CategoryId == categoryId);
    }

    public async Task<int> GetVariantCountAsync(int categoryId)
    {
        return await _db.Products
            .Where(p => p.CategoryId == categoryId)
            .SelectMany(p => p.Variants)
            .CountAsync();
    }

    public async Task<bool> HasProductsAsync(int categoryId)
    {
        return await _db.Products.AnyAsync(p => p.CategoryId == categoryId);
    }

    public async Task<List<SelectListItem>> GetSizeSystemSelectListAsync(int? selectedId = null)
    {
        var sizeSystems = await _db.SizeSystems
            .OrderBy(ss => ss.Name)
            .Select(ss => new SelectListItem
            {
                Value = ss.Id.ToString(),
                Text = $"{ss.Name} ({ss.SizeType})",
                Selected = selectedId.HasValue && ss.Id == selectedId.Value
            })
            .ToListAsync();

        sizeSystems.Insert(0, new SelectListItem
        {
            Value = "",
            Text = "-- No Default Size System --"
        });

        return sizeSystems;
    }

    public async Task<List<CategoryProductInfo>> GetCategoryProductsAsync(int categoryId, int take = 5)
    {
        return await _db.Products
            .Where(p => p.CategoryId == categoryId)
            .Select(p => new CategoryProductInfo { Id = p.Id, Name = p.Name })
            .Take(take)
            .ToListAsync();
    }
}
