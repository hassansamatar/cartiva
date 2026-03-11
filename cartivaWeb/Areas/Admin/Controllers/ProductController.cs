using DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.ViewModels;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostEnvironment;

        public ProductController(ApplicationDbContext db, IWebHostEnvironment hostEnvironment)
        {
            _db = db;
            _hostEnvironment = hostEnvironment;
        }

        // ======================
        // GET: Product List
        // ======================
        public IActionResult Index()
        {
            List<Product> productList = _db.Producties
                .Include(p => p.Category)
                .AsNoTracking()
                .ToList();

            return View(productList);
        }

        // ======================
        // GET: Upsert
        // ======================
        public IActionResult Upsert(int? id)
        {
            ProductVM productVM = new()
            {
                Product = new Product(),
                CategoryList = _db.Categories
                    .Select(u => new SelectListItem
                    {
                        Text = u.Name,
                        Value = u.Id.ToString()
                    })
            };

            if (id == null || id == 0)
            {
                return View(productVM);
            }

            productVM.Product = _db.Producties
                .Include(p => p.Category)
                .FirstOrDefault(p => p.Id == id);

            if (productVM.Product == null)
            {
                return NotFound();
            }

            return View(productVM);
        }

        // ======================
        // POST: Upsert
        // ======================
        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, IFormFile? file)
        {
            if (ModelState.IsValid)
            {
                string wwwRootPath = _hostEnvironment.WebRootPath;

                if (file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                    string productPath = Path.Combine(wwwRootPath, "images/products");

                    // delete old image
                    if (!string.IsNullOrEmpty(productVM.Product.ImageUrl))
                    {
                        var oldImagePath = Path.Combine(wwwRootPath, productVM.Product.ImageUrl.TrimStart('\\'));

                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // upload new image
                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))
                    {
                        file.CopyTo(fileStream);
                    }

                    productVM.Product.ImageUrl = @"\images\products\" + fileName;
                }

                if (productVM.Product.Id == 0)
                {
                    _db.Producties.Add(productVM.Product);
                }
                else
                {
                    _db.Producties.Update(productVM.Product);
                }

                _db.SaveChanges();

                return RedirectToAction(nameof(Index));
            }

            productVM.CategoryList = _db.Categories
                .Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                });

            return View(productVM);
        }

        // ======================
        // GET: Delete
        // ======================
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }

            Product? productFromDb = _db.Producties
                .Include(p => p.Category)
                .FirstOrDefault(p => p.Id == id);

            if (productFromDb == null)
            {
                return NotFound();
            }

            return View(productFromDb);
        }

        // ======================
        // POST: Delete
        // ======================
        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePOST(int? id)
        {
            Product? obj = _db.Producties.Find(id);

            if (obj == null)
            {
                return NotFound();
            }

            _db.Producties.Remove(obj);
            _db.SaveChanges();

            return RedirectToAction(nameof(Index));
        }
    }
}