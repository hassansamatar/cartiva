using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Infrastructure.ImageServices;
using Cartiva.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Cartiva.Application.Services;

/// <summary>
/// Service for managing products and product variants
/// </summary>
public class ProductService : IProductService
{
    private readonly ApplicationDbContext _db;
    private readonly IImageService _imageService;

    private static readonly List<string> ValidColors = new()
    {
        "Red", "Blue", "Green", "Black", "White", "Navy", "Gray", "Brown", "Tan", "Pink", "Yellow"
    };

    public ProductService(ApplicationDbContext db, IImageService imageService)
    {
        _db = db;
        _imageService = imageService;
    }

    #region Products

    public async Task<List<Product>> GetAllProductsAsync()
    {
        return await _db.Products
            .Include(p => p.Category)
                .ThenInclude(c => c.DefaultSizeSystem)
            .Include(p => p.Variants)
                .ThenInclude(v => v.SizeValue)
                    .ThenInclude(sv => sv!.SizeSystem)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Product?> GetProductByIdAsync(int id)
    {
        return await _db.Products
            .Include(p => p.Variants)
                .ThenInclude(v => v.SizeValue)
                    .ThenInclude(sv => sv!.SizeSystem)
            .Include(p => p.Category)
                .ThenInclude(c => c.DefaultSizeSystem)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<ProductOperationResult> CreateProductAsync(Product product, IFormFile? imageFile)
    {
        if (imageFile != null)
        {
            product.ImageUrl = await _imageService.SaveImage(imageFile);
        }

        await _db.Products.AddAsync(product);
        await _db.SaveChangesAsync();

        return ProductOperationResult.Succeeded("Product created successfully", product.Id);
    }

    public async Task<ProductOperationResult> UpdateProductAsync(Product product, IFormFile? imageFile)
    {
        var productFromDb = await _db.Products.FindAsync(product.Id);

        if (productFromDb == null)
            return ProductOperationResult.Failed("Product not found");

        productFromDb.Name = product.Name;
        productFromDb.Brand = product.Brand;
        productFromDb.Description = product.Description;
        productFromDb.CategoryId = product.CategoryId;

        if (imageFile != null)
        {
            productFromDb.ImageUrl = await _imageService.SaveImage(imageFile);
        }
        else if (!string.IsNullOrEmpty(product.ImageUrl))
        {
            productFromDb.ImageUrl = product.ImageUrl;
        }

        _db.Products.Update(productFromDb);
        await _db.SaveChangesAsync();

        return ProductOperationResult.Succeeded("Product updated successfully", product.Id);
    }

    public async Task<ProductOperationResult> DeleteProductAsync(int id)
    {
        var product = await _db.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
            return ProductOperationResult.Failed("Product not found");

        if (product.Variants != null && product.Variants.Any())
        {
            return ProductOperationResult.Failed(
                "Cannot delete product because it has variants. Delete the variants first.");
        }

        _imageService.DeleteImage(product.ImageUrl);

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return ProductOperationResult.Succeeded("Product deleted successfully");
    }

    public async Task<List<SelectListItem>> GetCategorySelectListAsync()
    {
        return await _db.Categories
            .Select(c => new SelectListItem
            {
                Text = c.Name,
                Value = c.Id.ToString()
            })
            .ToListAsync();
    }

    public async Task<CategorySizeSystemInfo?> GetCategorySizeSystemAsync(int categoryId)
    {
        var category = await _db.Categories
            .Include(c => c.DefaultSizeSystem)
            .FirstOrDefaultAsync(c => c.Id == categoryId);

        if (category?.DefaultSizeSystem != null)
        {
            return new CategorySizeSystemInfo
            {
                HasSizeSystem = true,
                SizeSystemName = category.DefaultSizeSystem.Name,
                SizeSystemId = category.DefaultSizeSystem.Id,
                IconClass = category.DefaultSizeSystem.IconClass,
                AlertClass = category.DefaultSizeSystem.AlertClass
            };
        }

        return new CategorySizeSystemInfo { HasSizeSystem = false };
    }

    #endregion

    #region Variants

    public async Task<List<ProductVariant>> GetVariantsByProductIdAsync(int productId)
    {
        return await _db.ProductVariants
            .Include(v => v.SizeValue)
                .ThenInclude(sv => sv!.SizeSystem)
            .Where(v => v.ProductId == productId)
            .ToListAsync();
    }

    public async Task<ProductVariant?> GetVariantByIdAsync(int id)
    {
        return await _db.ProductVariants
            .Include(v => v.Product)
                .ThenInclude(p => p.Category)
                    .ThenInclude(c => c.DefaultSizeSystem)
            .Include(v => v.SizeValue)
                .ThenInclude(sv => sv!.SizeSystem)
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<ProductOperationResult> CreateVariantAsync(ProductVariant variant)
    {
        var validation = await ValidateVariantAsync(variant, isUpdate: false);
        if (!validation.IsValid)
        {
            return ProductOperationResult.ValidationFailed(validation.Errors);
        }

        await _db.ProductVariants.AddAsync(variant);
        await _db.SaveChangesAsync();

        return ProductOperationResult.Succeeded("Variant added successfully", variant.Id);
    }

    public async Task<ProductOperationResult> UpdateVariantAsync(ProductVariant variant)
    {
        var validation = await ValidateVariantAsync(variant, isUpdate: true);
        if (!validation.IsValid)
        {
            return ProductOperationResult.ValidationFailed(validation.Errors);
        }

        _db.ProductVariants.Update(variant);
        await _db.SaveChangesAsync();

        return ProductOperationResult.Succeeded("Variant updated successfully", variant.Id);
    }

    public async Task<ProductOperationResult> DeleteVariantAsync(int id)
    {
        var variant = await _db.ProductVariants
            .Include(v => v.Product)
            .Include(v => v.SizeValue)
            .FirstOrDefaultAsync(v => v.Id == id);

        if (variant == null)
            return ProductOperationResult.Failed("Variant not found or already deleted.");

        var variantInfo = $"{variant.Color} - {variant.SizeValue?.DisplayText ?? "No Size"}";

        try
        {
            _db.ProductVariants.Remove(variant);
            await _db.SaveChangesAsync();

            return ProductOperationResult.Succeeded($"Variant ({variantInfo}) deleted successfully");
        }
        catch (Exception ex)
        {
            return ProductOperationResult.Failed($"Error deleting variant: {ex.Message}");
        }
    }

    public async Task<List<SelectListItem>> GetAvailableSizesAsync(int productId)
    {
        var product = await _db.Products
            .Include(p => p.Category)
                .ThenInclude(c => c.DefaultSizeSystem)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product?.Category?.DefaultSizeSystem == null)
        {
            return new List<SelectListItem>();
        }

        return await _db.SizeValues
            .Where(sv => sv.SizeSystemId == product.Category.DefaultSizeSystem.Id)
            .OrderBy(sv => sv.SortOrder)
            .Select(sv => new SelectListItem
            {
                Value = sv.Id.ToString(),
                Text = sv.DisplayText
            })
            .ToListAsync();
    }

    public List<SelectListItem> GetColorSelectList(string? selectedColor = null)
    {
        return ValidColors.Select(c => new SelectListItem
        {
            Value = c,
            Text = c,
            Selected = c == selectedColor
        }).ToList();
    }

    public async Task<VariantValidationResult> ValidateVariantAsync(ProductVariant variant, bool isUpdate = false)
    {
        var errors = new Dictionary<string, string>();

        // Validate color
        if (!ValidColors.Contains(variant.Color))
        {
            errors["Variant.Color"] = "Please select a valid color.";
        }

        // Validate SizeValueId if provided
        if (variant.SizeValueId.HasValue)
        {
            var sizeValue = await _db.SizeValues
                .FirstOrDefaultAsync(sv => sv.Id == variant.SizeValueId.Value);

            if (sizeValue == null)
            {
                errors["Variant.SizeValueId"] = "Please select a valid size.";
            }
        }

        // Check for duplicate variant
        bool variantExists;
        if (variant.SizeValueId.HasValue)
        {
            variantExists = await _db.ProductVariants
                .AnyAsync(v => v.ProductId == variant.ProductId
                            && v.SizeValueId == variant.SizeValueId
                            && v.Color == variant.Color
                            && (!isUpdate || v.Id != variant.Id));
        }
        else
        {
            variantExists = await _db.ProductVariants
                .AnyAsync(v => v.ProductId == variant.ProductId
                            && v.SizeValueId == null
                            && v.Color == variant.Color
                            && (!isUpdate || v.Id != variant.Id));
        }

        if (variantExists)
        {
            errors[""] = "A variant with this color already exists for this product.";
        }

        if (errors.Any())
        {
            return new VariantValidationResult { IsValid = false, Errors = errors };
        }

        return VariantValidationResult.Valid();
    }

    #endregion
}
