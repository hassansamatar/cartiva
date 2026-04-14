using Cartiva.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cartiva.Domain;
using Cartiva.Shared;
using System.Security.Claims;

namespace CartivaWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ReviewController(ApplicationDbContext db)
        {
            _db = db;
        }

        // POST: /Customer/Review/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productVariantId, int orderId, int rating, string? comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Verify the user actually ordered this variant and it's delivered
            var orderDetail = await _db.OrderDetails
                .Include(od => od.OrderHeader)
                .FirstOrDefaultAsync(od => od.OrderHeader.Id == orderId
                    && od.ProductVariantId == productVariantId
                    && od.OrderHeader.ApplicationUserId == userId
                    && od.OrderHeader.OrderStatus == SD.StatusDelivered);

            if (orderDetail == null)
            {
                TempData["error"] = "You can only review products from delivered orders.";
                return RedirectToAction("Details", "Order", new { area = "Customer", id = orderId });
            }

            // Check if user already reviewed this variant for this order
            var existingReview = await _db.Reviews
                .AnyAsync(r => r.ApplicationUserId == userId
                    && r.ProductVariantId == productVariantId);

            if (existingReview)
            {
                TempData["error"] = "You have already reviewed this product.";
                return RedirectToAction("Details", "Order", new { area = "Customer", id = orderId });
            }

            var review = new Review
            {
                ApplicationUserId = userId,
                ProductVariantId = productVariantId,
                Rating = rating,
                Comment = comment?.Trim(),
                ReviewDate = DateTime.UtcNow,
                IsApproved = false
            };

            _db.Reviews.Add(review);
            await _db.SaveChangesAsync();

            TempData["success"] = "Thank you! Your review has been submitted and is pending approval.";
            return RedirectToAction("Details", "Order", new { area = "Customer", id = orderId });
        }
    }
}
