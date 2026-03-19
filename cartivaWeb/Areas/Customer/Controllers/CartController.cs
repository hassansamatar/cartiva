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

        // GET: Get cart count for navbar badge
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { count = 0 });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var count = await _db.ShoppingCarts
                .Where(c => c.ApplicationUserId == userId)
                .SumAsync(c => c.Count);

            return Json(new { count });
        }

        // Add item to cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productVariantId, int count = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var variant = await _db.ProductVariants
                .Include(v => v.Product)
                .Include(v => v.SizeValue)
                .FirstOrDefaultAsync(v => v.Id == productVariantId);

            if (variant == null) return NotFound();

            // Current quantity in cart for this variant
            var cartItem = await _db.ShoppingCarts
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId && c.ProductVariantId == productVariantId);

            int totalRequested = count + (cartItem?.Count ?? 0);

            if (totalRequested > variant.Stock)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Cannot add {count} items. Only {variant.Stock - (cartItem?.Count ?? 0)} left in stock."
                    });
                }

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

            // Get updated cart count
            var cartCount = await _db.ShoppingCarts
                .Where(c => c.ApplicationUserId == userId)
                .SumAsync(c => c.Count);

            string sizeDisplay = variant.SizeValue != null
                ? variant.SizeValue.DisplayText
                : "No Size";

            // If it's an AJAX request, return JSON
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = true,
                    cartCount = cartCount,
                    message = $"{variant.Product?.Name} ({variant.Color}/{sizeDisplay}) added to your cart!"
                });
            }

            TempData["Success"] = $"{variant.Product?.Name} ({variant.Color}/{sizeDisplay}) added to your cart!";
            return RedirectToAction("Details", "Home", new { id = variant.ProductId });
        }

        // Increment quantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Increment(int id)
        {
            var cartItem = await _db.ShoppingCarts
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.SizeValue)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Cart item not found." });
            }

            if (cartItem.Count >= cartItem.ProductVariant.Stock)
            {
                return Json(new
                {
                    success = false,
                    message = $"Cannot add more than {cartItem.ProductVariant.Stock} in stock."
                });
            }

            cartItem.Count++;
            await _db.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartCount = await _db.ShoppingCarts
                .Where(c => c.ApplicationUserId == userId)
                .SumAsync(c => c.Count);

            string sizeDisplay = cartItem.ProductVariant.SizeValue != null
                ? cartItem.ProductVariant.SizeValue.DisplayText
                : "No Size";

            return Json(new
            {
                success = true,
                newCount = cartItem.Count,
                cartCount = cartCount,
                subtotal = (cartItem.ProductVariant.Price * cartItem.Count).ToString("C"),
                message = $"Increased quantity of {cartItem.ProductVariant.Product?.Name} ({cartItem.ProductVariant.Color}/{sizeDisplay}) to {cartItem.Count}."
            });
        }

        // Decrement quantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decrement(int id)
        {
            var cartItem = await _db.ShoppingCarts
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.SizeValue)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Cart item not found." });
            }

            string productInfo = GetProductInfo(cartItem.ProductVariant);
            bool removed = false;

            cartItem.Count--;
            if (cartItem.Count <= 0)
            {
                _db.ShoppingCarts.Remove(cartItem);
                removed = true;
            }

            await _db.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartCount = await _db.ShoppingCarts
                .Where(c => c.ApplicationUserId == userId)
                .SumAsync(c => c.Count);

            if (removed)
            {
                return Json(new
                {
                    success = true,
                    removed = true,
                    itemId = id,
                    cartCount = cartCount,
                    message = $"{productInfo} removed from your cart."
                });
            }

            return Json(new
            {
                success = true,
                newCount = cartItem.Count,
                cartCount = cartCount,
                subtotal = (cartItem.ProductVariant.Price * cartItem.Count).ToString("C"),
                message = $"Decreased quantity of {productInfo} to {cartItem.Count}."
            });
        }

        // Update quantity directly
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCount(int id, int count)
        {
            var cartItem = await _db.ShoppingCarts
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.SizeValue)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Cart item not found." });
            }

            string productInfo = GetProductInfo(cartItem.ProductVariant);
            bool removed = false;

            if (count <= 0)
            {
                _db.ShoppingCarts.Remove(cartItem);
                removed = true;
            }
            else if (count > cartItem.ProductVariant.Stock)
            {
                return Json(new
                {
                    success = false,
                    message = $"Cannot set quantity higher than available stock ({cartItem.ProductVariant.Stock})."
                });
            }
            else
            {
                cartItem.Count = count;
            }

            await _db.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartCount = await _db.ShoppingCarts
                .Where(c => c.ApplicationUserId == userId)
                .SumAsync(c => c.Count);

            if (removed)
            {
                return Json(new
                {
                    success = true,
                    removed = true,
                    itemId = id,
                    cartCount = cartCount,
                    message = $"{productInfo} removed from your cart."
                });
            }

            return Json(new
            {
                success = true,
                newCount = cartItem.Count,
                cartCount = cartCount,
                subtotal = (cartItem.ProductVariant.Price * cartItem.Count).ToString("C"),
                message = $"Updated quantity of {productInfo} to {count}."
            });
        }

        // Remove single item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var cartItem = await _db.ShoppingCarts
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.SizeValue)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cartItem == null)
            {
                return Json(new { success = false, message = "Cart item not found." });
            }

            string productInfo = GetProductInfo(cartItem.ProductVariant);

            _db.ShoppingCarts.Remove(cartItem);
            await _db.SaveChangesAsync();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartCount = await _db.ShoppingCarts
                .Where(c => c.ApplicationUserId == userId)
                .SumAsync(c => c.Count);

            return Json(new
            {
                success = true,
                removed = true,
                itemId = id,
                cartCount = cartCount,
                message = $"{productInfo} removed from your cart."
            });
        }

        // Remove all items
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAll()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = await _db.ShoppingCarts
                .Where(c => c.ApplicationUserId == userId)
                .ToListAsync();

            _db.ShoppingCarts.RemoveRange(cartItems);
            await _db.SaveChangesAsync();

            return Json(new
            {
                success = true,
                cartCount = 0,
                message = "All items removed from your cart."
            });
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