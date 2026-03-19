using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using Models.Interfaces;
using MyUtility;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IImageService _imageService;

        public ProductController(ApplicationDbContext db, IImageService imageService)
        {
            _db = db;
            _imageService = imageService;
        }

        #region PRODUCT

        public async Task<IActionResult> Index()
        {
            var products = await _db.Products
                .Include(p => p.Category)
                    .ThenInclude(c => c.DefaultSizeSystem)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.SizeValue)
                        .ThenInclude(sv => sv.SizeSystem)
                .AsNoTracking()
                .ToListAsync();

            return View(products);
        }

        public async Task<IActionResult> Upsert(int? id)
        {
            ProductVM vm = new()
            {
                Product = new Product(),
                Variants = new List<ProductVariant>(),
                CategoryList = await _db.Categories
                    .Select(c => new SelectListItem
                    {
                        Text = c.Name,
                        Value = c.Id.ToString()
                    }).ToListAsync()
            };

            if (id == null || id == 0)
                return View(vm);

            var product = await _db.Products
                .Include(p => p.Variants)
                    .ThenInclude(v => v.SizeValue)
                        .ThenInclude(sv => sv.SizeSystem)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            vm.Product = product;
            vm.Variants = product.Variants.ToList();

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upsert(ProductVM vm, IFormFile? file)
        {
            ModelState.Remove("Product.Category");
            ModelState.Remove("Product.Variants");

            if (!ModelState.IsValid)
            {
                vm.CategoryList = await _db.Categories
                    .Select(c => new SelectListItem
                    {
                        Text = c.Name,
                        Value = c.Id.ToString()
                    }).ToListAsync();

                return View(vm);
            }

            if (file != null)
                vm.Product.ImageUrl = await _imageService.SaveImage(file);

            if (vm.Product.Id == 0)
            {
                await _db.Products.AddAsync(vm.Product);
                TempData["success"] = "Product created successfully";
            }
            else
            {
                var productFromDb = await _db.Products.FindAsync(vm.Product.Id);

                if (productFromDb == null)
                    return NotFound();

                productFromDb.Name = vm.Product.Name;
                productFromDb.Brand = vm.Product.Brand;
                productFromDb.Description = vm.Product.Description;
                productFromDb.CategoryId = vm.Product.CategoryId;

                if (!string.IsNullOrEmpty(vm.Product.ImageUrl))
                    productFromDb.ImageUrl = vm.Product.ImageUrl;

                _db.Products.Update(productFromDb);

                TempData["success"] = "Product updated successfully";
            }

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id)
        {
            var product = await _db.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            // Check if product has variants
            if (product.Variants != null && product.Variants.Any())
            {
                TempData["error"] = "Cannot delete product because it has variants. Delete the variants first.";
                return RedirectToAction(nameof(Index));
            }

            _imageService.DeleteImage(product.ImageUrl);

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            TempData["success"] = "Product deleted successfully";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region VARIANTS

        public async Task<IActionResult> VariantIndex(int productId)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                    .ThenInclude(c => c.DefaultSizeSystem)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return NotFound();

            var variants = await _db.ProductVariants
                .Include(v => v.SizeValue)
                    .ThenInclude(sv => sv.SizeSystem)
                .Where(v => v.ProductId == productId)
                .ToListAsync();

            ViewBag.ProductName = product.Name;
            ViewBag.ProductId = productId;
            ViewBag.CategoryName = product.Category?.Name ?? "";
            ViewBag.SizeSystem = product.Category?.DefaultSizeSystem;

            return View(variants);
        }

        // GET: Create Product Variant
        public async Task<IActionResult> CreateProductVariant(int productId)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                    .ThenInclude(c => c.DefaultSizeSystem)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            // Get the size system for this product's category
            var sizeSystem = product.Category?.DefaultSizeSystem;

            var vm = new ProductVariantVM
            {
                Variant = new ProductVariant
                {
                    ProductId = productId
                },
                AvailableColors = GetColorSelectList(),
                ProductName = product.Name,
                SizeSystem = sizeSystem
            };

            // Only load sizes if category has a size system
            if (sizeSystem != null)
            {
                vm.AvailableSizes = await _db.SizeValues
                    .Where(sv => sv.SizeSystemId == sizeSystem.Id)
                    .OrderBy(sv => sv.SortOrder)
                    .Select(sv => new SelectListItem
                    {
                        Value = sv.Id.ToString(),
                        Text = sv.DisplayText
                    })
                    .ToListAsync();
            }
            else
            {
                vm.AvailableSizes = new List<SelectListItem>();
                // Don't show error - accessories category can have products without sizes
            }

            return View(vm);
        }

        // POST: Create Product Variant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProductVariant(ProductVariantVM vm)
        {
            ModelState.Remove("Variant.Product");
            ModelState.Remove("AvailableColors");
            ModelState.Remove("AvailableSizes");
            ModelState.Remove("SizeSystem");
            ModelState.Remove("Variant.SizeValue");

            // Validate color
            var validColors = GetColorList();
            if (!validColors.Contains(vm.Variant.Color))
            {
                ModelState.AddModelError("Variant.Color", "Please select a valid color.");
            }

            // Only validate SizeValueId if it has a value
            if (vm.Variant.SizeValueId.HasValue)
            {
                var sizeValue = await _db.SizeValues
                    .FirstOrDefaultAsync(sv => sv.Id == vm.Variant.SizeValueId.Value);

                if (sizeValue == null)
                {
                    ModelState.AddModelError("Variant.SizeValueId", "Please select a valid size.");
                }
            }

            // Check for duplicate variant (handles both sized and non-sized products)
            bool variantExists;
            if (vm.Variant.SizeValueId.HasValue)
            {
                variantExists = await _db.ProductVariants
                    .AnyAsync(v => v.ProductId == vm.Variant.ProductId
                                && v.SizeValueId == vm.Variant.SizeValueId
                                && v.Color == vm.Variant.Color);
            }
            else
            {
                variantExists = await _db.ProductVariants
                    .AnyAsync(v => v.ProductId == vm.Variant.ProductId
                                && v.SizeValueId == null
                                && v.Color == vm.Variant.Color);
            }

            if (variantExists)
            {
                ModelState.AddModelError("", "A variant with this color already exists for this product.");
            }

            if (!ModelState.IsValid)
            {
                // Repopulate the form
                var product = await _db.Products
                    .Include(p => p.Category)
                        .ThenInclude(c => c.DefaultSizeSystem)
                    .FirstOrDefaultAsync(p => p.Id == vm.Variant.ProductId);

                vm.ProductName = product?.Name;
                vm.SizeSystem = product?.Category?.DefaultSizeSystem;
                vm.AvailableColors = GetColorSelectList(vm.Variant.Color);

                if (product?.Category?.DefaultSizeSystem != null)
                {
                    vm.AvailableSizes = await _db.SizeValues
                        .Where(sv => sv.SizeSystemId == product.Category.DefaultSizeSystem.Id)
                        .OrderBy(sv => sv.SortOrder)
                        .Select(sv => new SelectListItem
                        {
                            Value = sv.Id.ToString(),
                            Text = sv.DisplayText
                        })
                        .ToListAsync();
                }
                else
                {
                    vm.AvailableSizes = new List<SelectListItem>();
                }

                return View(vm);
            }

            await _db.ProductVariants.AddAsync(vm.Variant);
            await _db.SaveChangesAsync();

            TempData["success"] = "Variant added successfully";
            return RedirectToAction(nameof(VariantIndex), new { productId = vm.Variant.ProductId });
        }

        // GET: Edit Product Variant
        public async Task<IActionResult> EditProductVariant(int id)
        {
            var variant = await _db.ProductVariants
                .Include(v => v.Product)
                    .ThenInclude(p => p.Category)
                        .ThenInclude(c => c.DefaultSizeSystem)
                .Include(v => v.SizeValue)
                    .ThenInclude(sv => sv.SizeSystem)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (variant == null)
                return NotFound();

            var sizeSystem = variant.Product?.Category?.DefaultSizeSystem;

            var vm = new ProductVariantVM
            {
                Variant = variant,
                AvailableColors = GetColorSelectList(variant.Color),
                ProductName = variant.Product?.Name,
                SizeSystem = sizeSystem
            };

            // Only load sizes if category has a size system
            if (sizeSystem != null)
            {
                vm.AvailableSizes = await _db.SizeValues
                    .Where(sv => sv.SizeSystemId == sizeSystem.Id)
                    .OrderBy(sv => sv.SortOrder)
                    .Select(sv => new SelectListItem
                    {
                        Value = sv.Id.ToString(),
                        Text = sv.DisplayText,
                        Selected = sv.Id == variant.SizeValueId
                    })
                    .ToListAsync();
            }
            else
            {
                vm.AvailableSizes = new List<SelectListItem>();
            }

            return View(vm);
        }

        // POST: Edit Product Variant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProductVariant(ProductVariantVM vm)
        {
            ModelState.Remove("Variant.Product");
            ModelState.Remove("AvailableColors");
            ModelState.Remove("AvailableSizes");
            ModelState.Remove("SizeSystem");
            ModelState.Remove("Variant.SizeValue");

            // Validate color
            var validColors = GetColorList();
            if (!validColors.Contains(vm.Variant.Color))
            {
                ModelState.AddModelError("Variant.Color", "Please select a valid color.");
            }

            // Only validate SizeValueId if it has a value
            if (vm.Variant.SizeValueId.HasValue)
            {
                var sizeValue = await _db.SizeValues
                    .FirstOrDefaultAsync(sv => sv.Id == vm.Variant.SizeValueId.Value);

                if (sizeValue == null)
                {
                    ModelState.AddModelError("Variant.SizeValueId", "Please select a valid size.");
                }
            }

            // Check for duplicate variant (handles both sized and non-sized)
            bool variantExists;
            if (vm.Variant.SizeValueId.HasValue)
            {
                variantExists = await _db.ProductVariants
                    .AnyAsync(v => v.ProductId == vm.Variant.ProductId
                                && v.SizeValueId == vm.Variant.SizeValueId
                                && v.Color == vm.Variant.Color
                                && v.Id != vm.Variant.Id);
            }
            else
            {
                variantExists = await _db.ProductVariants
                    .AnyAsync(v => v.ProductId == vm.Variant.ProductId
                                && v.SizeValueId == null
                                && v.Color == vm.Variant.Color
                                && v.Id != vm.Variant.Id);
            }

            if (variantExists)
            {
                ModelState.AddModelError("", "A variant with this color already exists for this product.");
            }

            if (!ModelState.IsValid)
            {
                // Repopulate the form
                var product = await _db.Products
                    .Include(p => p.Category)
                        .ThenInclude(c => c.DefaultSizeSystem)
                    .FirstOrDefaultAsync(p => p.Id == vm.Variant.ProductId);

                vm.ProductName = product?.Name;
                vm.SizeSystem = product?.Category?.DefaultSizeSystem;
                vm.AvailableColors = GetColorSelectList(vm.Variant.Color);

                if (product?.Category?.DefaultSizeSystem != null)
                {
                    vm.AvailableSizes = await _db.SizeValues
                        .Where(sv => sv.SizeSystemId == product.Category.DefaultSizeSystem.Id)
                        .OrderBy(sv => sv.SortOrder)
                        .Select(sv => new SelectListItem
                        {
                            Value = sv.Id.ToString(),
                            Text = sv.DisplayText,
                            Selected = sv.Id == vm.Variant.SizeValueId
                        })
                        .ToListAsync();
                }
                else
                {
                    vm.AvailableSizes = new List<SelectListItem>();
                }

                return View(vm);
            }

            _db.ProductVariants.Update(vm.Variant);
            await _db.SaveChangesAsync();

            TempData["success"] = "Variant updated successfully";
            return RedirectToAction(nameof(VariantIndex), new { productId = vm.Variant.ProductId });
        }

        // GET: Delete Product Variant - Shows confirmation page
        public async Task<IActionResult> DeleteProductVariant(int id)
        {
            var variant = await _db.ProductVariants
                .Include(v => v.Product)
                .Include(v => v.SizeValue)
                    .ThenInclude(sv => sv.SizeSystem)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (variant == null)
            {
                TempData["error"] = "Variant not found.";
                return RedirectToAction(nameof(Index), "Product");
            }

            ViewBag.ProductName = variant.Product?.Name ?? "Unknown Product";

            return View(variant);
        }

        // POST: Delete Product Variant - Performs the actual deletion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProductVariantConfirmed(int id)  // Fixed typo
        {
            var variant = await _db.ProductVariants
                .Include(v => v.Product)
                .Include(v => v.SizeValue)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (variant == null)
            {
                TempData["error"] = "Variant not found or already deleted.";
                return RedirectToAction(nameof(Index), "Product");
            }

            int productId = variant.ProductId;
            string productName = variant.Product?.Name ?? "Unknown";
            string variantInfo = $"{variant.Color} - {variant.SizeValue?.DisplayText ?? "No Size"}";

            try
            {
                _db.ProductVariants.Remove(variant);
                await _db.SaveChangesAsync();

                TempData["success"] = $"Variant ({variantInfo}) deleted successfully";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting variant {id}: {ex.Message}");
                TempData["error"] = "An error occurred while deleting the variant. Please try again.";
                return RedirectToAction(nameof(DeleteProductVariant), new { id });
            }

            return RedirectToAction(nameof(VariantIndex), new { productId });
        }

        #endregion

        #region Helper Methods

        private List<string> GetColorList()
        {
            return new List<string> { "Red", "Blue", "Green", "Black", "White", "Navy", "Gray", "Brown", "Tan", "Pink", "Yellow" };
        }

        private List<SelectListItem> GetColorSelectList(string? selectedColor = null)
        {
            return GetColorList().Select(c => new SelectListItem
            {
                Value = c,
                Text = c,
                Selected = c == selectedColor
            }).ToList();
        }

        [HttpGet]
        public async Task<IActionResult> GetCategorySizeSystem(int categoryId)
        {
            var category = await _db.Categories
                .Include(c => c.DefaultSizeSystem)
                .FirstOrDefaultAsync(c => c.Id == categoryId);

            if (category?.DefaultSizeSystem != null)
            {
                return Json(new
                {
                    hasSizeSystem = true,
                    sizeSystemName = category.DefaultSizeSystem.Name,
                    sizeSystemId = category.DefaultSizeSystem.Id,
                    iconClass = category.DefaultSizeSystem.IconClass,
                    alertClass = category.DefaultSizeSystem.AlertClass
                });
            }

            return Json(new { hasSizeSystem = false });
        }

        #endregion
    }
}