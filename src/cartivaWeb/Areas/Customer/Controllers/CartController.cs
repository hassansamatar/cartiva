using Cartiva.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CartivaWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        // Display shopping cart
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var cartItems = await _cartService.GetCartItemsAsync(userId);
            var totals = await _cartService.CalculateTotalsAsync(userId);

            ViewBag.PromotionDiscount = new
            {
                TotalDiscount = totals.TotalDiscount,
                AppliedPromotions = totals.AppliedPromotions
            };

            return View(cartItems);
        }

        // GET: Recalculate promotion discount for AJAX calls
        [HttpGet]
        public async Task<IActionResult> GetPromotionDiscount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var totals = await _cartService.CalculateTotalsAsync(userId);

            return Json(new
            {
                subtotal = totals.SubtotalIncVat,
                totalDiscount = totals.TotalDiscount,
                finalTotal = totals.FinalTotal,
                promotions = totals.AppliedPromotions.Select(p => new
                {
                    p.DisplayText,
                    p.CategoryName,
                    p.Discount,
                    p.FreeItemCount
                })
            });
        }

        // GET: Get cart count for navbar badge
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            if (!User.Identity?.IsAuthenticated ?? true)
            {
                return Json(new { count = 0 });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var count = await _cartService.GetCartCountAsync(userId);

            return Json(new { count });
        }

        // Add item to cart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productVariantId, int count = 1)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _cartService.AddToCartAsync(userId, productVariantId, count);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    success = result.Success,
                    cartCount = result.CartCount,
                    message = result.Message
                });
            }

            if (result.Success)
            {
                TempData["Success"] = result.Message;
            }
            else
            {
                TempData["Error"] = result.Message;
            }

            // Get productId for redirect - need to fetch variant
            return RedirectToAction("Index", "Home");
        }

        // Increment quantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Increment(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _cartService.IncrementAsync(userId, id);

            return Json(new
            {
                success = result.Success,
                newCount = result.NewItemCount,
                cartCount = result.CartCount,
                subtotal = result.ItemSubtotal?.ToString("C"),
                message = result.Message
            });
        }

        // Decrement quantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Decrement(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _cartService.DecrementAsync(userId, id);

            return Json(new
            {
                success = result.Success,
                removed = result.ItemRemoved,
                itemId = result.RemovedItemId,
                newCount = result.NewItemCount,
                cartCount = result.CartCount,
                subtotal = result.ItemSubtotal?.ToString("C"),
                message = result.Message
            });
        }

        // Update quantity directly
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCount(int id, int count)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _cartService.UpdateCountAsync(userId, id, count);

            return Json(new
            {
                success = result.Success,
                removed = result.ItemRemoved,
                itemId = result.RemovedItemId,
                newCount = result.NewItemCount,
                cartCount = result.CartCount,
                subtotal = result.ItemSubtotal?.ToString("C"),
                message = result.Message
            });
        }

        // Remove single item
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var result = await _cartService.RemoveFromCartAsync(userId, id);

            return Json(new
            {
                success = result.Success,
                removed = result.ItemRemoved,
                itemId = result.RemovedItemId,
                cartCount = result.CartCount,
                message = result.Message
            });
        }

        // Remove all items
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveAll()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _cartService.ClearCartAsync(userId);

            return Json(new
            {
                success = true,
                cartCount = 0,
                message = "All items removed from your cart."
            });
        }
    }
}