using Cartiva.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cartiva.Shared;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        // GET: /Admin/Review/Index
        public async Task<IActionResult> Index()
        {
            var reviews = await _reviewService.GetAllReviewsAsync();
            return View(reviews);
        }

        // POST: /Admin/Review/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _reviewService.ApproveReviewAsync(id);

            if (result.Success)
                TempData["success"] = result.Message;
            else
                TempData["error"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Review/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var result = await _reviewService.RejectReviewAsync(id);

            if (result.Success)
                TempData["success"] = result.Message;
            else
                TempData["error"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Review/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _reviewService.DeleteReviewAsync(id);

            if (result.Success)
                TempData["success"] = result.Message;
            else
                TempData["error"] = result.Message;

            return RedirectToAction(nameof(Index));
        }
    }
}
