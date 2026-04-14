using Cartiva.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cartiva.Shared;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ReviewController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Admin/Review/Index
        public async Task<IActionResult> Index()
        {
            var reviews = await _db.Reviews
                .Include(r => r.ApplicationUser)
                .Include(r => r.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .OrderByDescending(r => r.ReviewDate)
                .ToListAsync();

            return View(reviews);
        }

        // POST: /Admin/Review/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var review = await _db.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            review.IsApproved = true;
            await _db.SaveChangesAsync();

            TempData["success"] = "Review approved.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Review/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var review = await _db.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            review.IsApproved = false;
            await _db.SaveChangesAsync();

            TempData["success"] = "Review rejected.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Review/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _db.Reviews.FindAsync(id);
            if (review == null) return NotFound();

            _db.Reviews.Remove(review);
            await _db.SaveChangesAsync();

            TempData["success"] = "Review deleted.";
            return RedirectToAction(nameof(Index));
        }
    }
}
