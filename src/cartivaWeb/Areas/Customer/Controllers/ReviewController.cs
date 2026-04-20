using Cartiva.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CartivaWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // POST: /Customer/Review/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int productVariantId, int orderId, int rating, string? comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var result = await _reviewService.CreateReviewAsync(userId, productVariantId, orderId, rating, comment);

            if (result.Success)
            {
                TempData["success"] = result.Message;
            }
            else
            {
                TempData["error"] = result.Message;
            }

            return RedirectToAction("Details", "Order", new { area = "Customer", id = orderId });
        }
    }
}
