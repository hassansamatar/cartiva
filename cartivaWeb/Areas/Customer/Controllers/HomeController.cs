using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;

namespace CartivaWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        // List all products
        public IActionResult Index()
        {
            var products = _db.Products
                .Include(p => p.Category)
                .ToList();

            return View(products);
        }

        // Product details with variants
        public IActionResult Details(int id)
        {
            var product = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // Add a product variant to shopping cart
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToCart(int productVariantId, int count = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
                return RedirectToAction("Login", "Account");

            var variant = await _db.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == productVariantId);

            if (variant == null)
                return NotFound();

            if (count > variant.Stock)
                return BadRequest("Not enough stock available.");

            var cartItem = await _db.ShoppingCarts
                .FirstOrDefaultAsync(c => c.ApplicationUserId == userId && c.ProductVariantId == productVariantId);

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
            TempData["Success"] = $"{variant.Product.Name} ({variant.Color}/{variant.Size}) added to your cart!";
            return RedirectToAction("Details", new { id = variant.ProductId });
        }

        // Show shopping cart
        [Authorize]
        public async Task<IActionResult> Cart()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var cartItems = await _db.ShoppingCarts
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                .Where(c => c.ApplicationUserId == userId)
                .ToListAsync();

            return View(cartItems);
        }

        // Update cart item quantity directly
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateCount(int id, int count)
        {
            var cartItem = await _db.ShoppingCarts
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cartItem != null)
            {
                if (count <= 0)
                {
                    _db.ShoppingCarts.Remove(cartItem);
                    TempData["Success"] = $"{cartItem.ProductVariant.Product.Name} removed from your cart.";
                }
                else
                {
                    cartItem.Count = count;
                    TempData["Success"] = $"Updated quantity of {cartItem.ProductVariant.Product.Name} to {count}.";
                }

                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Cart");
        }

        // Increment cart item
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Increment(int id)
        {
            var cartItem = await _db.ShoppingCarts
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cartItem != null && cartItem.Count < cartItem.ProductVariant.Stock)
            {
                cartItem.Count += 1;
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Increased quantity of {cartItem.ProductVariant.Product.Name} to {cartItem.Count}.";
            }

            return RedirectToAction("Cart");
        }

        // Decrement cart item
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Decrement(int id)
        {
            var cartItem = await _db.ShoppingCarts
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cartItem != null)
            {
                cartItem.Count -= 1;
                if (cartItem.Count <= 0)
                {
                    _db.ShoppingCarts.Remove(cartItem);
                    TempData["Success"] = $"{cartItem.ProductVariant.Product.Name} removed from your cart.";
                }
                else
                {
                    TempData["Success"] = $"Decreased quantity of {cartItem.ProductVariant.Product.Name} to {cartItem.Count}.";
                }

                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Cart");
        }

        // Remove cart item
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Remove(int id)
        {
            var cartItem = await _db.ShoppingCarts
                .Include(c => c.ProductVariant)
                    .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cartItem != null)
            {
                _db.ShoppingCarts.Remove(cartItem);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"{cartItem.ProductVariant.Product.Name} removed from your cart.";
            }

            return RedirectToAction("Cart");
        }

        // Remove all items from cart
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> RemoveAll()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cartItems = await _db.ShoppingCarts
                .Where(c => c.ApplicationUserId == userId)
                .ToListAsync();

            _db.ShoppingCarts.RemoveRange(cartItems);
            await _db.SaveChangesAsync();
            TempData["Success"] = "All items removed from your cart.";

            return RedirectToAction("Cart");
        }

        // Privacy page
        public IActionResult Privacy()
        {
            return View();
        }

        // Error page
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}