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
                .Include(p => p.Variants)
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
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeletePost(int id)
        {
            var product = await _db.Products.FindAsync(id);

            if (product == null)
                return NotFound();

            _imageService.DeleteImage(product.ImageUrl);

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            TempData["success"] = "Product deleted";

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region VARIANTS

        public async Task<IActionResult> VariantIndex(int productId)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
                return NotFound();

            var variants = await _db.ProductVariants
                .Where(v => v.ProductId == productId)
                .ToListAsync();

            ViewBag.ProductName = product.Name;
            ViewBag.ProductId = productId;
            ViewBag.CategoryName = product.Category?.Name ?? "";

            return View(variants);
        }

        // GET: Create Product Variant
        public async Task<IActionResult> CreateProductVariant(int productId, string sizeType = "Regular")
        {
            var product = await _db.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            // Determine size group based on category
            string sizeGroup = "Adult"; // default
            if (product.Category.Name.Contains("Kids", StringComparison.OrdinalIgnoreCase) ||
                product.Category.Name.Contains("Children", StringComparison.OrdinalIgnoreCase))
            {
                sizeGroup = "Kid";
            }

            // Choose sizes based on sizeGroup and sizeType
            List<SelectListItem> sizes;
            if (sizeGroup == "Kid")
            {
                sizes = ProductVariantOptions.KidSizes;
            }
            else // Adult
            {
                sizes = sizeType == "Suit"
                    ? ProductVariantOptions.AdultSuitSizes
                    : ProductVariantOptions.AdultSizes;
            }

            var vm = new ProductVariantVM
            {
                Variant = new ProductVariant { ProductId = productId },
                AvailableColors = ProductVariantOptions.Colors,
                AvailableSizes = sizes
            };

            ViewBag.ProductName = product.Name;
            ViewBag.SizeGroup = sizeGroup;
            ViewBag.SizeType = sizeType;
            ViewBag.CategoryName = product.Category.Name;

            return View(vm);
        }

        // POST: Create Product Variant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProductVariant(ProductVariantVM vm, string sizeGroup, string sizeType)
        {
            ModelState.Remove("Variant.Product");

            // If sizeGroup is null or empty, default to Adult
            if (string.IsNullOrEmpty(sizeGroup))
            {
                sizeGroup = "Adult";
            }

            // Get valid sizes based on sizeGroup and sizeType
            List<string> validSizes;
            if (sizeGroup == "Kid")
            {
                validSizes = ProductVariantOptions.KidSizes.Select(s => s.Value).ToList();
            }
            else // Adult
            {
                validSizes = sizeType == "Suit"
                    ? ProductVariantOptions.AdultSuitSizes.Select(s => s.Value).ToList()
                    : ProductVariantOptions.AdultSizes.Select(s => s.Value).ToList();
            }

            var validColors = ProductVariantOptions.Colors.Select(c => c.Value).ToList();

            // Validate the selected size and color
            if (!validColors.Contains(vm.Variant.Color))
            {
                ModelState.AddModelError("Variant.Color", "Please select a valid color.");
            }

            if (!validSizes.Contains(vm.Variant.Size))
            {
                ModelState.AddModelError("Variant.Size", "Please select a valid size.");
            }

            // Check if variant with same size and color already exists
            bool variantExists = await _db.ProductVariants
                .AnyAsync(v => v.ProductId == vm.Variant.ProductId
                            && v.Size == vm.Variant.Size
                            && v.Color == vm.Variant.Color);

            if (variantExists)
            {
                ModelState.AddModelError("", "A variant with this size and color already exists for this product.");
            }

            if (!ModelState.IsValid)
            {
                // Repopulate the dropdowns
                vm.AvailableColors = ProductVariantOptions.Colors;

                if (sizeGroup == "Kid")
                {
                    vm.AvailableSizes = ProductVariantOptions.KidSizes;
                }
                else
                {
                    vm.AvailableSizes = sizeType == "Suit"
                        ? ProductVariantOptions.AdultSuitSizes
                        : ProductVariantOptions.AdultSizes;
                }

                var product = await _db.Products.FindAsync(vm.Variant.ProductId);
                ViewBag.ProductName = product?.Name;
                ViewBag.SizeGroup = sizeGroup;
                ViewBag.SizeType = sizeType;

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
                .FirstOrDefaultAsync(v => v.Id == id);

            if (variant == null)
                return NotFound();

            // Determine size group based on category
            string sizeGroup = "Adult";
            string sizeType = "Regular"; // Default

            if (variant.Product.Category.Name.Contains("Kids", StringComparison.OrdinalIgnoreCase) ||
                variant.Product.Category.Name.Contains("Children", StringComparison.OrdinalIgnoreCase))
            {
                sizeGroup = "Kid";
            }
            else
            {
                // Determine sizeType based on the variant's size value
                if (ProductVariantOptions.AdultSuitSizes.Any(s => s.Value == variant.Size))
                {
                    sizeType = "Suit";
                }
            }

            // Choose sizes based on sizeGroup and sizeType
            List<SelectListItem> sizes;
            if (sizeGroup == "Kid")
            {
                sizes = ProductVariantOptions.KidSizes;
            }
            else // Adult
            {
                sizes = sizeType == "Suit"
                    ? ProductVariantOptions.AdultSuitSizes
                    : ProductVariantOptions.AdultSizes;
            }

            var vm = new ProductVariantVM
            {
                Variant = variant,
                AvailableColors = ProductVariantOptions.Colors,
                AvailableSizes = sizes
            };

            ViewBag.ProductName = variant.Product?.Name;
            ViewBag.SizeGroup = sizeGroup;
            ViewBag.SizeType = sizeType;

            return View(vm);
        }

        // POST: Edit Product Variant
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProductVariant(ProductVariantVM vm, string sizeGroup, string sizeType)
        {
            ModelState.Remove("Variant.Product");
            ModelState.Remove("AvailableColors");
            ModelState.Remove("AvailableSizes");

            // If sizeGroup is null or empty, default to Adult
            if (string.IsNullOrEmpty(sizeGroup))
            {
                sizeGroup = "Adult";
            }

            // Get valid sizes based on sizeGroup and sizeType
            List<string> validSizes;
            if (sizeGroup == "Kid")
            {
                validSizes = ProductVariantOptions.KidSizes.Select(s => s.Value).ToList();
            }
            else // Adult
            {
                validSizes = sizeType == "Suit"
                    ? ProductVariantOptions.AdultSuitSizes.Select(s => s.Value).ToList()
                    : ProductVariantOptions.AdultSizes.Select(s => s.Value).ToList();
            }

            var validColors = ProductVariantOptions.Colors.Select(c => c.Value).ToList();

            // Validate the selected size and color
            if (!validColors.Contains(vm.Variant.Color))
            {
                ModelState.AddModelError("Variant.Color", "Please select a valid color.");
            }

            if (!validSizes.Contains(vm.Variant.Size))
            {
                ModelState.AddModelError("Variant.Size", "Please select a valid size.");
            }

            // Check if another variant with same size and color already exists (excluding current variant)
            bool variantExists = await _db.ProductVariants
                .AnyAsync(v => v.ProductId == vm.Variant.ProductId
                            && v.Size == vm.Variant.Size
                            && v.Color == vm.Variant.Color
                            && v.Id != vm.Variant.Id);

            if (variantExists)
            {
                ModelState.AddModelError("", "A variant with this size and color already exists for this product.");
            }

            if (!ModelState.IsValid)
            {
                // Repopulate the dropdowns
                vm.AvailableColors = ProductVariantOptions.Colors;

                if (sizeGroup == "Kid")
                {
                    vm.AvailableSizes = ProductVariantOptions.KidSizes;
                }
                else
                {
                    vm.AvailableSizes = sizeType == "Suit"
                        ? ProductVariantOptions.AdultSuitSizes
                        : ProductVariantOptions.AdultSizes;
                }

                var product = await _db.Products.FindAsync(vm.Variant.ProductId);
                ViewBag.ProductName = product?.Name;
                ViewBag.SizeGroup = sizeGroup;
                ViewBag.SizeType = sizeType;

                return View(vm);
            }

            _db.ProductVariants.Update(vm.Variant);
            await _db.SaveChangesAsync();

            TempData["success"] = "Variant updated successfully";
            return RedirectToAction(nameof(VariantIndex), new { productId = vm.Variant.ProductId });
        }

        // POST: Delete Product Variant
        public async Task<IActionResult> DeleteProductVariant(int id)
        {
            var variant = await _db.ProductVariants.FindAsync(id);

            if (variant == null)
                return NotFound();

            int productId = variant.ProductId;

            _db.ProductVariants.Remove(variant);
            await _db.SaveChangesAsync();

            TempData["success"] = "Variant deleted";

            return RedirectToAction(nameof(VariantIndex), new { productId });
        }

        #endregion
    }
}