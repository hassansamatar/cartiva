using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Models;
using ApplicationUtility;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class PromotionController : Controller
    {
        private readonly ApplicationDbContext _db;

        public PromotionController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var promotions = await _db.Promotions
                .Include(p => p.Category)
                .OrderByDescending(p => p.IsActive)
                .ThenByDescending(p => p.EndDate)
                .AsNoTracking()
                .ToListAsync();

            return View(promotions);
        }

        public async Task<IActionResult> Upsert(int? id)
        {
            ViewBag.CategoryList = await GetCategorySelectListAsync();

            if (id == null || id == 0)
            {
                return View(new Promotion
                {
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now.AddDays(30),
                    BuyQuantity = 3,
                    GetQuantity = 1
                });
            }

            var promotion = await _db.Promotions.FindAsync(id);
            if (promotion == null) return NotFound();

            return View(promotion);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(Promotion promotion)
        {
            if (promotion.GetQuantity >= promotion.BuyQuantity)
            {
                ModelState.AddModelError("GetQuantity", "Free quantity must be less than buy quantity.");
            }

            if (promotion.EndDate <= promotion.StartDate)
            {
                ModelState.AddModelError("EndDate", "End date must be after start date.");
            }

            ModelState.Remove("Category");

            if (!ModelState.IsValid)
            {
                ViewBag.CategoryList = await GetCategorySelectListAsync(promotion.CategoryId);
                return View(promotion);
            }

            if (promotion.Id == 0)
            {
                _db.Promotions.Add(promotion);
                TempData["success"] = "Promotion created successfully.";
            }
            else
            {
                _db.Promotions.Update(promotion);
                TempData["success"] = "Promotion updated successfully.";
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var promotion = await _db.Promotions.FindAsync(id);
            if (promotion == null) return NotFound();

            _db.Promotions.Remove(promotion);
            await _db.SaveChangesAsync();

            TempData["success"] = "Promotion deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var promotion = await _db.Promotions.FindAsync(id);
            if (promotion == null) return NotFound();

            promotion.IsActive = !promotion.IsActive;
            await _db.SaveChangesAsync();

            TempData["success"] = $"Promotion {(promotion.IsActive ? "activated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<List<SelectListItem>> GetCategorySelectListAsync(int? selectedId = null)
        {
            return await _db.Categories
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name,
                    Selected = selectedId.HasValue && c.Id == selectedId.Value
                })
                .ToListAsync();
        }
    }
}
