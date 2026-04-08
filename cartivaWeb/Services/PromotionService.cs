using DataAccess;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Interfaces;

namespace Services
{
    public class PromotionService : IPromotionService
    {
        private readonly ApplicationDbContext _db;

        public PromotionService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<PromotionDiscount> CalculateDiscountAsync(IEnumerable<ShoppingCart> cartItems)
        {
            var result = new PromotionDiscount();

            var activePromotions = await _db.Promotions
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now)
                .AsNoTracking()
                .ToListAsync();

            if (!activePromotions.Any())
                return result;

            foreach (var promo in activePromotions)
            {
                // Get all cart items in this promotion's category
                var qualifyingItems = cartItems
                    .Where(c => c.ProductVariant?.Product?.CategoryId == promo.CategoryId)
                    .ToList();

                // Total quantity of items in this category
                int totalQty = qualifyingItems.Sum(c => c.Count);

                // Group size = paid items + free items (e.g. Buy 2 Get 1 Free = group of 3)
                int groupSize = promo.BuyQuantity + promo.GetQuantity;

                // How many full groups can be formed?
                int fullGroups = totalQty / groupSize;

                if (fullGroups <= 0)
                    continue;

                // Number of free items
                int freeItems = fullGroups * promo.GetQuantity;

                // Expand items into individual units sorted by price ascending (cheapest first)
                var unitPrices = qualifyingItems
                    .SelectMany(c => Enumerable.Repeat(c.ProductVariant.Price, c.Count))
                    .OrderBy(p => p)
                    .ToList();

                // The cheapest items are free
                decimal discount = unitPrices.Take(freeItems).Sum();

                if (discount > 0)
                {
                    result.TotalDiscount += discount;
                    result.AppliedPromotions.Add(new PromotionApplied
                    {
                        PromotionName = promo.Name,
                        DisplayText = promo.DisplayText,
                        CategoryName = promo.Category?.Name ?? "",
                        Discount = discount,
                        FreeItemCount = freeItems
                    });
                }
            }

            return result;
        }
    }
}
