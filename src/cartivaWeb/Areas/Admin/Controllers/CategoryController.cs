using Cartiva.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cartiva.Domain;
using Cartiva.Shared;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
        {
            _categoryService = categoryService;
            _logger = logger;
        }

        #region INDEX

        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();

                // Calculate statistics for each category
                var categoryStats = new Dictionary<int, (int ProductCount, int VariantCount)>();
                foreach (var category in categories)
                {
                    var productCount = await _categoryService.GetProductCountAsync(category.Id);
                    var variantCount = await _categoryService.GetVariantCountAsync(category.Id);
                    categoryStats[category.Id] = (productCount, variantCount);
                }

                ViewBag.CategoryStats = categoryStats;
                return View(categories);
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
                ViewBag.SizeSystemList = await _categoryService.GetSizeSystemSelectListAsync();
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
                    var result = await _categoryService.CreateCategoryAsync(obj);

                    if (result.Success)
                    {
                        TempData["success"] = result.Message;
                        return RedirectToAction(nameof(Index));
                    }

                    foreach (var error in result.ValidationErrors)
                    {
                        ModelState.AddModelError(error.Key, error.Value);
                    }
                }

                ViewBag.SizeSystemList = await _categoryService.GetSizeSystemSelectListAsync(obj.SizeSystemId);
                return View(obj);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                TempData["error"] = "An error occurred while creating the category.";
                ViewBag.SizeSystemList = await _categoryService.GetSizeSystemSelectListAsync(obj.SizeSystemId);
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
                    return NotFound();

                var category = await _categoryService.GetCategoryByIdAsync(id.Value);
                if (category == null)
                    return NotFound();

                ViewBag.ProductCount = await _categoryService.GetProductCountAsync(id.Value);
                ViewBag.VariantCount = await _categoryService.GetVariantCountAsync(id.Value);
                ViewBag.SizeSystemList = await _categoryService.GetSizeSystemSelectListAsync(category.SizeSystemId);

                return View(category);
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
                    var result = await _categoryService.UpdateCategoryAsync(obj);

                    if (result.Success)
                    {
                        TempData["success"] = result.Message;
                        return RedirectToAction(nameof(Index));
                    }

                    foreach (var error in result.ValidationErrors)
                    {
                        ModelState.AddModelError(error.Key, error.Value);
                    }

                    if (!string.IsNullOrEmpty(result.Message) && !result.ValidationErrors.Any())
                    {
                        TempData["error"] = result.Message;
                        return RedirectToAction(nameof(Edit), new { id = obj.Id });
                    }
                }

                ViewBag.SizeSystemList = await _categoryService.GetSizeSystemSelectListAsync(obj.SizeSystemId);
                ViewBag.ProductCount = await _categoryService.GetProductCountAsync(obj.Id);
                ViewBag.VariantCount = await _categoryService.GetVariantCountAsync(obj.Id);
                return View(obj);
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
                    return NotFound();

                var category = await _categoryService.GetCategoryByIdAsync(id.Value);
                if (category == null)
                    return NotFound();

                bool hasProducts = await _categoryService.HasProductsAsync(id.Value);
                ViewBag.HasProducts = hasProducts;

                if (hasProducts)
                {
                    ViewBag.ProductCount = await _categoryService.GetProductCountAsync(id.Value);
                    ViewBag.ProductList = await _categoryService.GetCategoryProductsAsync(id.Value, 5);
                }

                return View(category);
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
                    return NotFound();

                var result = await _categoryService.DeleteCategoryAsync(id.Value);

                if (result.Success)
                    TempData["success"] = result.Message;
                else
                    TempData["error"] = result.Message;

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
    }
}