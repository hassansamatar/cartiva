using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using System.Security.Claims;

namespace CartivaWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CartController(ApplicationDbContext db)
        {
            _db = db;
        }

        // Display shopping cart
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _db.ShoppingCarts
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Category)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.SizeValue)
                        .ThenInclude(sv => sv.SizeSystem)
                .Where(c => c.ApplicationUserId == userId)
                .ToListAsync();

            return View(cartItems);
        }

        // Add item to cart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productVariantId, int count = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var variant = await _db.ProductVariants
                .Include(v => v.Product)
                .Include(v => v.SizeValue)  // Add this to load size info
                .FirstOrDefaultAsync(v => v.Id == productVariantId);

            if (variant == null) return NotFound();

            // Current quantity in cart for this variant
            var cartItem = await _db.ShoppingCarts
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId && c.ProductVariantId == productVariantId);

            int totalRequested = count + (cartItem?.Count ?? 0);

            if (totalRequested > variant.Stock)
            {
                TempData["Error"] = $"Cannot add {count} items. Only {variant.Stock - (cartItem?.Count ?? 0)} left in stock.";
                return RedirectToAction("Details", "Home", new { id = variant.ProductId });
            }

            if (cartItem != null)
                cartItem.Count += count;
            else
            {
                cartItem = new ShoppingCart
                {
                    ApplicationUserId = userId,
                    ProductVariantId = productVariantId,
                    Count = count
                };
                _db.ShoppingCarts.Add(cartItem);
            }

            await _db.SaveChangesAsync();

            // Fix: Use DisplayText instead of SizeValue object
            string sizeDisplay = variant.SizeValue != null
                ? variant.SizeValue.DisplayText
                : "No Size";

            TempData["Success"] = $"{variant.Product?.Name} ({variant.Color}/{sizeDisplay}) added to your cart!";

            return RedirectToAction("Details", "Home", new { id = variant.ProductId });
        }

        // Increment quantity
        [HttpPost]
        public async Task<IActionResult> Increment(int id)
        {
            var cartItem = await _db.ShoppingCarts
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.SizeValue)  // Add this for size display
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cartItem != null)
            {
                if (cartItem.Count < cartItem.ProductVariant.Stock)
                {
                    cartItem.Count++;
                    await _db.SaveChangesAsync();

                    string sizeDisplay = cartItem.ProductVariant.SizeValue != null
                        ? cartItem.ProductVariant.SizeValue.DisplayText
                        : "No Size";

                    TempData["Success"] = $"Increased quantity of {cartItem.ProductVariant.Product?.Name} ({cartItem.ProductVariant.Color}/{sizeDisplay}) to {cartItem.Count}.";
                }
                else
                {
                    TempData["Error"] = $"Cannot add more than {cartItem.ProductVariant.Stock} in stock.";
                }
            }

            return RedirectToAction("Index");
        }

        // Decrement quantity
        [HttpPost]
        public async Task<IActionResult> Decrement(int id)
        {
            var cartItem = await _db.ShoppingCarts
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.SizeValue)  // Add this for size display
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cartItem != null)
            {
                string productInfo = GetProductInfo(cartItem.ProductVariant);

                cartItem.Count--;
                if (cartItem.Count <= 0)
                {
                    _db.ShoppingCarts.Remove(cartItem);
                    TempData["Success"] = $"{productInfo} removed from your cart.";
                }
                else
                {
                    TempData["Success"] = $"Decreased quantity of {productInfo} to {cartItem.Count}.";
                }

                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // Update quantity directly
        [HttpPost]
        public async Task<IActionResult> UpdateCount(int id, int count)
        {
            var cartItem = await _db.ShoppingCarts
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.SizeValue)  // Add this for size display
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cartItem != null)
            {
                string productInfo = GetProductInfo(cartItem.ProductVariant);

                if (count <= 0)
                {
                    _db.ShoppingCarts.Remove(cartItem);
                    TempData["Success"] = $"{productInfo} removed from your cart.";
                }
                else if (count > cartItem.ProductVariant.Stock)
                {
                    TempData["Error"] = $"Cannot set quantity higher than available stock ({cartItem.ProductVariant.Stock}).";
                }
                else
                {
                    cartItem.Count = count;
                    TempData["Success"] = $"Updated quantity of {productInfo} to {count}.";
                }

                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Index");
        }

        // Remove single item
        [HttpPost]
        public async Task<IActionResult> Remove(int id)
        {
            var cartItem = await _db.ShoppingCarts
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.SizeValue)  // Add this for size display
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cartItem != null)
            {
                string productInfo = GetProductInfo(cartItem.ProductVariant);

                _db.ShoppingCarts.Remove(cartItem);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"{productInfo} removed from your cart.";
            }

            return RedirectToAction("Index");
        }

        // Remove all items
        [HttpPost]
        public async Task<IActionResult> RemoveAll()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = await _db.ShoppingCarts
                .Where(c => c.ApplicationUserId == userId)
                .ToListAsync();

            _db.ShoppingCarts.RemoveRange(cartItems);
            await _db.SaveChangesAsync();
            TempData["Success"] = "All items removed from your cart.";

            return RedirectToAction("Index");
        }

        // Helper method to get product info with size display
        private string GetProductInfo(ProductVariant variant)
        {
            string sizeDisplay = variant.SizeValue != null
                ? variant.SizeValue.DisplayText
                : "No Size";

            return $"{variant.Product?.Name} ({variant.Color}/{sizeDisplay})";
        }
    }
}