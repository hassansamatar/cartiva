using DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using Models.ViewModels;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ProductController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            List<Product> productList = _db.Producties.ToList();
          

            return View(productList);
        }
        public IActionResult Create()
        {
            ProductVM productVM = new ProductVM()
            {
                Product = new Product(),
                CategoryList = _db.Categories.Select(u => new SelectListItem
                {
                    Text = u.Name,
                    Value = u.Id.ToString()
                })
            };

          
            return View(productVM);
        }
        [HttpPost]
        public IActionResult Create(ProductVM obj)
        {
            if (ModelState.IsValid)
            {
                // defensive: ensure EF will let the database generate the identity value
                //obj.Id = 0;
                _db.Producties.Add(obj.Product);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View();
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Product? productFromDb = _db.Producties.Find(id);


            if (productFromDb == null)
            {
                return NotFound();
            }
            return View(productFromDb);
        }
        [HttpPost]
        public IActionResult Edit(Product obj)
        {


            if (ModelState.IsValid)
            {
                _db.Producties.Update(obj);
                _db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(obj);

        }
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            Product? productFromDb = _db.Producties.Find(id);


            if (productFromDb == null)
            {
                return NotFound();
            }
            return View(productFromDb);
        }
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
            return RedirectToAction("Index");
        }


    }
}
