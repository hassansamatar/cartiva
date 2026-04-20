using Cartiva.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Cartiva.Domain;
using Cartiva.Domain.ViewModels;
using Cartiva.Shared;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        #region PRODUCT

        public async Task<IActionResult> Index()
        {
            var products = await _productService.GetAllProductsAsync();
            return View(products);
        }

        public async Task<IActionResult> Upsert(int? id)
        {
            ProductVM vm = new()
            {
                Product = new Product(),
                Variants = new List<ProductVariant>(),
                CategoryList = await _productService.GetCategorySelectListAsync()
            };

            if (id == null || id == 0)
                return View(vm);

            var product = await _productService.GetProductByIdAsync(id.Value);

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
                vm.CategoryList = await _productService.GetCategorySelectListAsync();
                return View(vm);
            }

            ProductOperationResult result;

            if (vm.Product.Id == 0)
            {
                result = await _productService.CreateProductAsync(vm.Product, file);
                if (result.Success)
                    TempData["success"] = "Product created successfully";
            }
            else
            {
                result = await _productService.UpdateProductAsync(vm.Product, file);
                if (result.Success)
                    TempData["success"] = "Product updated successfully";
            }

            if (!result.Success)
            {
                TempData["error"] = result.Message;
                vm.CategoryList = await _productService.GetCategorySelectListAsync();
                return View(vm);
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int id)
        {
            var result = await _productService.DeleteProductAsync(id);

            if (result.Success)
                TempData["success"] = result.Message;
            else
                TempData["error"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        #endregion

        #region VARIANTS

        public async Task<IActionResult> VariantIndex(int productId)
        {
            var product = await _productService.GetProductByIdAsync(productId);

            if (product == null)
                return NotFound();

            var variants = await _productService.GetVariantsByProductIdAsync(productId);

            ViewBag.ProductName = product.Name;
            ViewBag.ProductId = productId;
            ViewBag.CategoryName = product.Category?.Name ?? "";
            ViewBag.SizeSystem = product.Category?.DefaultSizeSystem;

            return View(variants);
        }

        // GET: Create Product Variant
        public async Task<IActionResult> CreateProductVariant(int productId)
        {
            var product = await _productService.GetProductByIdAsync(productId);

            if (product == null) return NotFound();

            var sizeSystem = product.Category?.DefaultSizeSystem;

            var vm = new ProductVariantVM
            {
                Variant = new ProductVariant
                {
                    ProductId = productId
                },
                AvailableColors = _productService.GetColorSelectList(),
                ProductName = product.Name,
                SizeSystem = sizeSystem,
                AvailableSizes = await _productService.GetAvailableSizesAsync(productId)
            };

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

            // Validate using service
            var validation = await _productService.ValidateVariantAsync(vm.Variant, isUpdate: false);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    ModelState.AddModelError(error.Key, error.Value);
                }
            }

            if (!ModelState.IsValid)
            {
                return await RepopulateVariantForm(vm);
            }

            var result = await _productService.CreateVariantAsync(vm.Variant);

            if (result.Success)
            {
                TempData["success"] = "Variant added successfully";
                return RedirectToAction(nameof(VariantIndex), new { productId = vm.Variant.ProductId });
            }

            TempData["error"] = result.Message;
            return await RepopulateVariantForm(vm);
        }

        // GET: Edit Product Variant
        public async Task<IActionResult> EditProductVariant(int id)
        {
            var variant = await _productService.GetVariantByIdAsync(id);

            if (variant == null)
                return NotFound();

            var sizeSystem = variant.Product?.Category?.DefaultSizeSystem;

            var vm = new ProductVariantVM
            {
                Variant = variant,
                AvailableColors = _productService.GetColorSelectList(variant.Color),
                ProductName = variant.Product?.Name,
                SizeSystem = sizeSystem,
                AvailableSizes = await _productService.GetAvailableSizesAsync(variant.ProductId)
            };

            // Mark selected size
            foreach (var size in vm.AvailableSizes)
            {
                size.Selected = size.Value == variant.SizeValueId?.ToString();
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

            // Validate using service
            var validation = await _productService.ValidateVariantAsync(vm.Variant, isUpdate: true);
            if (!validation.IsValid)
            {
                foreach (var error in validation.Errors)
                {
                    ModelState.AddModelError(error.Key, error.Value);
                }
            }

            if (!ModelState.IsValid)
            {
                return await RepopulateVariantForm(vm);
            }

            var result = await _productService.UpdateVariantAsync(vm.Variant);

            if (result.Success)
            {
                TempData["success"] = "Variant updated successfully";
                return RedirectToAction(nameof(VariantIndex), new { productId = vm.Variant.ProductId });
            }

            TempData["error"] = result.Message;
            return await RepopulateVariantForm(vm);
        }

        // GET: Delete Product Variant - Shows confirmation page
        public async Task<IActionResult> DeleteProductVariant(int id)
        {
            var variant = await _productService.GetVariantByIdAsync(id);

            if (variant == null)
            {
                TempData["error"] = "Variant not found.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ProductName = variant.Product?.Name ?? "Unknown Product";

            return View(variant);
        }

        // POST: Delete Product Variant - Performs the actual deletion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProductVariantConfirmed(int id)
        {
            var variant = await _productService.GetVariantByIdAsync(id);

            if (variant == null)
            {
                TempData["error"] = "Variant not found or already deleted.";
                return RedirectToAction(nameof(Index));
            }

            int productId = variant.ProductId;

            var result = await _productService.DeleteVariantAsync(id);

            if (result.Success)
                TempData["success"] = result.Message;
            else
                TempData["error"] = result.Message;

            return RedirectToAction(nameof(VariantIndex), new { productId });
        }

        #endregion

        #region Helper Methods

        private async Task<IActionResult> RepopulateVariantForm(ProductVariantVM vm)
        {
            var product = await _productService.GetProductByIdAsync(vm.Variant.ProductId);

            vm.ProductName = product?.Name;
            vm.SizeSystem = product?.Category?.DefaultSizeSystem;
            vm.AvailableColors = _productService.GetColorSelectList(vm.Variant.Color);
            vm.AvailableSizes = await _productService.GetAvailableSizesAsync(vm.Variant.ProductId);

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetCategorySizeSystem(int categoryId)
        {
            var sizeInfo = await _productService.GetCategorySizeSystemAsync(categoryId);

            if (sizeInfo?.HasSizeSystem == true)
            {
                return Json(new
                {
                    hasSizeSystem = true,
                    sizeSystemName = sizeInfo.SizeSystemName,
                    sizeSystemId = sizeInfo.SizeSystemId,
                    iconClass = sizeInfo.IconClass,
                    alertClass = sizeInfo.AlertClass
                });
            }

            return Json(new { hasSizeSystem = false });
        }

        #endregion
    }
}