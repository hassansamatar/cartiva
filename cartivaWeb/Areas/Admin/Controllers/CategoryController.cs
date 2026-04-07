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

        public IActionResult Index()
        {
            try
            {
                // Include SizeSystem information when displaying categories
                List<Category> objCategoryList = _db.Categories
                    .Include(c => c.DefaultSizeSystem)  // Eager load the size system
                    .OrderBy(c => c.Name)  // Add ordering for consistency
                    .ToList();

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

        public IActionResult Create()
        {
            try
            {
                // Populate ViewBag with SizeSystems for dropdown
                ViewBag.SizeSystemList = GetSizeSystemSelectList();
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
        public IActionResult Create(Category obj)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Check if category with same name already exists
                    bool categoryExists = _db.Categories.Any(c => c.Name.ToLower() == obj.Name.ToLower());
                    if (categoryExists)
                    {
                        ModelState.AddModelError("Name", "A category with this name already exists.");
                        ViewBag.SizeSystemList = GetSizeSystemSelectList(obj.SizeSystemId);
                        return View(obj);
                    }

                    _db.Categories.Add(obj);
                    _db.SaveChanges();

                    _logger.LogInformation("Category created: {CategoryName} (ID: {CategoryId})", obj.Name, obj.Id);
                    TempData["success"] = $"Category '{obj.Name}' created successfully";
                    return RedirectToAction(nameof(Index));
                }

                // If validation fails, repopulate the dropdown
                ViewBag.SizeSystemList = GetSizeSystemSelectList(obj.SizeSystemId);
                return View(obj);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                TempData["error"] = "An error occurred while creating the category.";
                ViewBag.SizeSystemList = GetSizeSystemSelectList(obj.SizeSystemId);
                return View(obj);
            }
        }

        #endregion

        #region EDIT

        public IActionResult Edit(int? id)
        {
            try
            {
                if (id == null || id == 0)
                {
                    return NotFound();
                }

                Category? categoryFromDb = _db.Categories
                    .Include(c => c.DefaultSizeSystem)
                    .FirstOrDefault(c => c.Id == id);

                if (categoryFromDb == null)
                {
                    return NotFound();
                }

                // Get usage statistics for the view
                ViewBag.ProductCount = _db.Products.Count(p => p.CategoryId == id);
                ViewBag.VariantCount = _db.Products
                    .Where(p => p.CategoryId == id)
                    .SelectMany(p => p.Variants)
                    .Count();

                // Populate ViewBag with SizeSystems for dropdown, selecting the current one
                ViewBag.SizeSystemList = GetSizeSystemSelectList(categoryFromDb.SizeSystemId);

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
        public IActionResult Edit(Category obj)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Check if another category with same name already exists (excluding current)
                    bool categoryExists = _db.Categories
                        .Any(c => c.Name.ToLower() == obj.Name.ToLower() && c.Id != obj.Id);

                    if (categoryExists)
                    {
                        ModelState.AddModelError("Name", "A category with this name already exists.");
                        ViewBag.SizeSystemList = GetSizeSystemSelectList(obj.SizeSystemId);
                        ViewBag.ProductCount = _db.Products.Count(p => p.CategoryId == obj.Id);
                        ViewBag.VariantCount = _db.Products
                            .Where(p => p.CategoryId == obj.Id)
                            .SelectMany(p => p.Variants)
                            .Count();
                        return View(obj);
                    }

                    _db.Categories.Update(obj);
                    _db.SaveChanges();

                    _logger.LogInformation("Category updated: {CategoryName} (ID: {CategoryId})", obj.Name, obj.Id);
                    TempData["success"] = $"Category '{obj.Name}' updated successfully";
                    return RedirectToAction(nameof(Index));
                }

                // If validation fails, repopulate the dropdown
                ViewBag.SizeSystemList = GetSizeSystemSelectList(obj.SizeSystemId);
                ViewBag.ProductCount = _db.Products.Count(p => p.CategoryId == obj.Id);
                ViewBag.VariantCount = _db.Products
                    .Where(p => p.CategoryId == obj.Id)
                    .SelectMany(p => p.Variants)
                    .Count();
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

        public IActionResult Delete(int? id)
        {
            try
            {
                if (id == null || id == 0)
                {
                    return NotFound();
                }

                Category? categoryFromDb = _db.Categories
                    .Include(c => c.DefaultSizeSystem)
                    .FirstOrDefault(c => c.Id == id);

                if (categoryFromDb == null)
                {
                    return NotFound();
                }

                // Check if any products are using this category
                bool hasProducts = _db.Products.Any(p => p.CategoryId == id);
                ViewBag.HasProducts = hasProducts;

                if (hasProducts)
                {
                    ViewBag.ProductCount = _db.Products.Count(p => p.CategoryId == id);
                    ViewBag.ProductList = _db.Products
                        .Where(p => p.CategoryId == id)
                        .Select(p => new { p.Id, p.Name })
                        .Take(5)  // Limit to 5 for display
                        .ToList();
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
        public IActionResult DeletePOST(int? id)
        {
            try
            {
                if (id == null)
                {
                    return NotFound();
                }

                Category? obj = _db.Categories
                    .Include(c => c.DefaultSizeSystem)
                    .FirstOrDefault(c => c.Id == id);

                if (obj == null)
                {
                    return NotFound();
                }

                // Check if any products are using this category
                bool hasProducts = _db.Products.Any(p => p.CategoryId == id);
                if (hasProducts)
                {
                    int productCount = _db.Products.Count(p => p.CategoryId == id);
                    TempData["error"] = $"Cannot delete category '{obj.Name}' because it has {productCount} product(s) assigned to it.";
                    return RedirectToAction(nameof(Index));
                }

                string categoryName = obj.Name;

                _db.Categories.Remove(obj);
                _db.SaveChanges();

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
        private List<SelectListItem> GetSizeSystemSelectList(int? selectedId = null)
        {
            try
            {
                var sizeSystems = _db.SizeSystems
                    .OrderBy(ss => ss.Name)
                    .Select(ss => new SelectListItem
                    {
                        Value = ss.Id.ToString(),
                        Text = $"{ss.Name} ({ss.SizeType})",
                        Selected = selectedId.HasValue && ss.Id == selectedId.Value
                    })
                    .ToList();

                // Add a "None" option at the top
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