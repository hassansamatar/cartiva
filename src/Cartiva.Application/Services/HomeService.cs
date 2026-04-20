using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Cartiva.Application.Services;

/// <summary>
/// Service for customer-facing home/browsing operations
/// </summary>
public class HomeService : IHomeService
{
    private readonly ApplicationDbContext _db;

    public HomeService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Product>> GetAllProductsForBrowsingAsync()
    {
        return await _db.Products
            .Include(p => p.Category)
                .ThenInclude(c => c.DefaultSizeSystem)
            .Include(p => p.Variants)
                .ThenInclude(v => v.SizeValue)
                    .ThenInclude(sv => sv!.SizeSystem)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Reviews!.Where(r => r.IsApproved))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Product?> GetProductDetailsAsync(int productId)
    {
        return await _db.Products
            .Include(p => p.Category)
                .ThenInclude(c => c.DefaultSizeSystem)
            .Include(p => p.Variants)
                .ThenInclude(v => v.SizeValue)
                    .ThenInclude(sv => sv!.SizeSystem)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Reviews!.Where(r => r.IsApproved))
                    .ThenInclude(r => r.ApplicationUser)
            .FirstOrDefaultAsync(p => p.Id == productId);
    }

    public async Task<List<Promotion>> GetActivePromotionsAsync()
    {
        return await _db.Promotions
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Product>> SearchProductsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetAllProductsForBrowsingAsync();
        }

        var lowerSearch = searchTerm.ToLower();

        return await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
                .ThenInclude(v => v.SizeValue)
            .Where(p => p.Name.ToLower().Contains(lowerSearch)
                     || p.Description.ToLower().Contains(lowerSearch)
                     || p.Brand.ToLower().Contains(lowerSearch)
                     || p.Category.Name.ToLower().Contains(lowerSearch))
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Product>> GetProductsByCategoryAsync(int categoryId)
    {
        return await _db.Products
            .Include(p => p.Category)
                .ThenInclude(c => c.DefaultSizeSystem)
            .Include(p => p.Variants)
                .ThenInclude(v => v.SizeValue)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Reviews!.Where(r => r.IsApproved))
            .Where(p => p.CategoryId == categoryId)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Product>> GetFeaturedProductsAsync(int count = 8)
    {
        // Featured products: those with the most approved reviews
        return await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
                .ThenInclude(v => v.SizeValue)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Reviews!.Where(r => r.IsApproved))
            .OrderByDescending(p => p.Variants.SelectMany(v => v.Reviews!.Where(r => r.IsApproved)).Count())
            .Take(count)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Category>> GetCategoriesAsync()
    {
        return await _db.Categories
            .Include(c => c.DefaultSizeSystem)
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync();
    }
}
