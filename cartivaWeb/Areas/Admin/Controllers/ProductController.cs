using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;
using MyUtility;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _environment;

        public ProductController(ApplicationDbContext db, IWebHostEnvironment environment)
        {
            _db = db;
            _environment = environment;
        }

        #region PRODUCT

        // =========================
        // Product List
        // =========================
        public async Task<IActionResult> Index()
        {
            var products = await _db.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .AsNoTracking()
                .ToListAsync();

            return View(products);
        }

        // =========================
        // Upsert GET
        // =========================
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

        // =========================
        // Upsert POST
        // =========================
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
                vm.Product.ImageUrl = await SaveImage(file);

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

        // =========================
        // Delete
        // =========================
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

            DeleteImage(product.ImageUrl);

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();

            TempData["success"] = "Product deleted";

            return RedirectToAction(nameof(Index));
        }

        #endregion


        #region VARIANTS

        // =========================
        // Variant List
        // =========================
        public async Task<IActionResult> VariantIndex(int productId)
        {
            var product = await _db.Products.FindAsync(productId);

            if (product == null)
                return NotFound();

            var variants = await _db.ProductVariants
                .Where(v => v.ProductId == productId)
                .ToListAsync();

            ViewBag.ProductName = product.Name;
            ViewBag.ProductId = productId;

            return View(variants);
        }

        // =========================
        // Create Variant
        // =========================
        public async Task<IActionResult> VariantCreate(int productId)
        {
            var product = await _db.Products.FindAsync(productId);

            if (product == null)
                return NotFound();

            ViewBag.ProductName = product.Name;

            return View(new ProductVariant
            {
                ProductId = productId
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VariantCreate(ProductVariant variant)
        {
            ModelState.Remove("Product"); // FIX NOT NULL ERROR

            if (!ModelState.IsValid)
                return View(variant);

            await _db.ProductVariants.AddAsync(variant);
            await _db.SaveChangesAsync();

            TempData["success"] = "Variant added";

            return RedirectToAction(nameof(VariantIndex),
                new { productId = variant.ProductId });
        }

        // =========================
        // Edit Variant
        // =========================
        public async Task<IActionResult> VariantEdit(int id)
        {
            var variant = await _db.ProductVariants.FindAsync(id);

            if (variant == null)
                return NotFound();

            return View(variant);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VariantEdit(ProductVariant variant)
        {
            ModelState.Remove("Product");

            if (!ModelState.IsValid)
                return View(variant);

            _db.ProductVariants.Update(variant);

            await _db.SaveChangesAsync();

            TempData["success"] = "Variant updated";

            return RedirectToAction(nameof(VariantIndex),
                new { productId = variant.ProductId });
        }

        // =========================
        // Delete Variant
        // =========================
        public async Task<IActionResult> VariantDelete(int id)
        {
            var variant = await _db.ProductVariants.FindAsync(id);

            if (variant == null)
                return NotFound();

            int productId = variant.ProductId;

            _db.ProductVariants.Remove(variant);

            await _db.SaveChangesAsync();

            TempData["success"] = "Variant deleted";

            return RedirectToAction(nameof(VariantIndex),
                new { productId });
        }

        #endregion


        #region IMAGE HELPERS

        private async Task<string> SaveImage(IFormFile file)
        {
            string root = _environment.WebRootPath;
            string fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);

            string folder = Path.Combine(root, "images/products");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            string filePath = Path.Combine(folder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);

            await file.CopyToAsync(stream);

            return "/images/products/" + fileName;
        }

        private void DeleteImage(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return;

            string root = _environment.WebRootPath;

            string path = Path.Combine(root, imageUrl.TrimStart('/'));

            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        #endregion
    }
}