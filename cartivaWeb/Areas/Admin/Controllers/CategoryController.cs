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
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ApplicationDbContext db, ILogger<CategoryController> logger)
        {
            _db = db;
            _logger = logger;
        }

        #region INDEX

        public async Task<IActionResult> Index()
        {
            try
            {
                List<Category> objCategoryList = await _db.Categories
                    .Include(c => c.DefaultSizeSystem)
                    .OrderBy(c => c.Name)
                    .ToListAsync();

                return View(objCategoryList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading categories");
                TempData["error"] = "An error occurred while loading categories.";
                return View(new List<Category>());
            }
        }

        #endregion

        #region CREATE

        public async Task<IActionResult> Create()
        {
            try
            {
                ViewBag.SizeSystemList = await GetSizeSystemSelectListAsync();
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading create form");
                TempData["error"] = "An error occurred while loading the form.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category obj)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool categoryExists = await _db.Categories.AnyAsync(c => c.Name.ToLower() == obj.Name.ToLower());
                    if (categoryExists)
                    {
                        ModelState.AddModelError("Name", "A category with this name already exists.");
                        ViewBag.SizeSystemList = await GetSizeSystemSelectListAsync(obj.SizeSystemId);
                        return View(obj);
                    }

                    _db.Categories.Add(obj);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation("Category created: {CategoryName} (ID: {CategoryId})", obj.Name, obj.Id);
                    TempData["success"] = $"Category '{obj.Name}' created successfully";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.SizeSystemList = await GetSizeSystemSelectListAsync(obj.SizeSystemId);
                return View(obj);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                TempData["error"] = "An error occurred while creating the category.";
                ViewBag.SizeSystemList = await GetSizeSystemSelectListAsync(obj.SizeSystemId);
                return View(obj);
            }
        }

        #endregion

        #region EDIT

        public async Task<IActionResult> Edit(int? id)
        {
            try
            {
                if (id == null || id == 0)
                {
                    return NotFound();
                }

                Category? categoryFromDb = await _db.Categories
                    .Include(c => c.DefaultSizeSystem)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (categoryFromDb == null)
                {
                    return NotFound();
                }

                ViewBag.ProductCount = await _db.Products.CountAsync(p => p.CategoryId == id);
                ViewBag.VariantCount = await _db.Products
                    .Where(p => p.CategoryId == id)
                    .SelectMany(p => p.Variants)
                    .CountAsync();

                ViewBag.SizeSystemList = await GetSizeSystemSelectListAsync(categoryFromDb.SizeSystemId);

                return View(categoryFromDb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading edit form for category ID: {CategoryId}", id);
                TempData["error"] = "An error occurred while loading the category.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category obj)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    bool categoryExists = await _db.Categories
                        .AnyAsync(c => c.Name.ToLower() == obj.Name.ToLower() && c.Id != obj.Id);

                    if (categoryExists)
                    {
                        ModelState.AddModelError("Name", "A category with this name already exists.");
                        ViewBag.SizeSystemList = await GetSizeSystemSelectListAsync(obj.SizeSystemId);
                        ViewBag.ProductCount = await _db.Products.CountAsync(p => p.CategoryId == obj.Id);
                        ViewBag.VariantCount = await _db.Products
                            .Where(p => p.CategoryId == obj.Id)
                            .SelectMany(p => p.Variants)
                            .CountAsync();
                        return View(obj);
                    }

                    _db.Categories.Update(obj);
                    await _db.SaveChangesAsync();

                    _logger.LogInformation("Category updated: {CategoryName} (ID: {CategoryId})", obj.Name, obj.Id);
                    TempData["success"] = $"Category '{obj.Name}' updated successfully";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.SizeSystemList = await GetSizeSystemSelectListAsync(obj.SizeSystemId);
                ViewBag.ProductCount = await _db.Products.CountAsync(p => p.CategoryId == obj.Id);
                ViewBag.VariantCount = await _db.Products
                    .Where(p => p.CategoryId == obj.Id)
                    .SelectMany(p => p.Variants)
                    .CountAsync();
                return View(obj);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating category ID: {CategoryId}", obj.Id);
                TempData["error"] = "The category was modified by another user. Please try again.";
                return RedirectToAction(nameof(Edit), new { id = obj.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category ID: {CategoryId}", obj.Id);
                TempData["error"] = "An error occurred while updating the category.";
                return RedirectToAction(nameof(Edit), new { id = obj.Id });
            }
        }

        #endregion

        #region DELETE

        public async Task<IActionResult> Delete(int? id)
        {
            try
            {
                if (id == null || id == 0)
                {
                    return NotFound();
                }

                Category? categoryFromDb = await _db.Categories
                    .Include(c => c.DefaultSizeSystem)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (categoryFromDb == null)
                {
                    return NotFound();
                }

                bool hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id);
                ViewBag.HasProducts = hasProducts;

                if (hasProducts)
                {
                    ViewBag.ProductCount = await _db.Products.CountAsync(p => p.CategoryId == id);
                    ViewBag.ProductList = await _db.Products
                        .Where(p => p.CategoryId == id)
                        .Select(p => new { p.Id, p.Name })
                        .Take(5)
                        .ToListAsync();
                }

                return View(categoryFromDb);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading delete form for category ID: {CategoryId}", id);
                TempData["error"] = "An error occurred while loading the category.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePOST(int? id)
        {
            try
            {
                if (id == null)
                {
                    return NotFound();
                }

                Category? obj = await _db.Categories
                    .Include(c => c.DefaultSizeSystem)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (obj == null)
                {
                    return NotFound();
                }

                bool hasProducts = await _db.Products.AnyAsync(p => p.CategoryId == id);
                if (hasProducts)
                {
                    int productCount = await _db.Products.CountAsync(p => p.CategoryId == id);
                    TempData["error"] = $"Cannot delete category '{obj.Name}' because it has {productCount} product(s) assigned to it.";
                    return RedirectToAction(nameof(Index));
                }

                string categoryName = obj.Name;

                _db.Categories.Remove(obj);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Category deleted: {CategoryName} (ID: {CategoryId})", categoryName, id);
                TempData["success"] = $"Category '{categoryName}' deleted successfully";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting category ID: {CategoryId}", id);
                TempData["error"] = "Cannot delete this category because it is referenced by other records.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category ID: {CategoryId}", id);
                TempData["error"] = "An error occurred while deleting the category.";
                return RedirectToAction(nameof(Index));
            }
        }

        #endregion

        #region HELPER METHODS

        /// <summary>
        /// Gets the list of size systems for dropdown selection
        /// </summary>
        private async Task<List<SelectListItem>> GetSizeSystemSelectListAsync(int? selectedId = null)
        {
            try
            {
                var sizeSystems = await _db.SizeSystems
                    .OrderBy(ss => ss.Name)
                    .Select(ss => new SelectListItem
                    {
                        Value = ss.Id.ToString(),
                        Text = $"{ss.Name} ({ss.SizeType})",
                        Selected = selectedId.HasValue && ss.Id == selectedId.Value
                    })
                    .ToListAsync();

                sizeSystems.Insert(0, new SelectListItem
                {
                    Value = "",
                    Text = "-- No Default Size System --"
                });

                return sizeSystems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading size systems for dropdown");
                return new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "-- No Size Systems Available --" }
                };
            }
        }

        /// <summary>
        /// Checks if a category exists by name (case insensitive)
        /// </summary>
        private bool CategoryExists(string name, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return _db.Categories.Any(c => c.Name.ToLower() == name.ToLower() && c.Id != excludeId.Value);
            }
            return _db.Categories.Any(c => c.Name.ToLower() == name.ToLower());
        }

        #endregion
    }
}