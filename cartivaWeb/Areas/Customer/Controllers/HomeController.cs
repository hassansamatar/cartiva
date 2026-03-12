using DataAccess;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using System.Diagnostics;

namespace CartivaWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
       

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
            
        }

        public IActionResult Index()
        {
            var products = _db.Products
       .Include(p => p.Category)
       .ToList();
          

            return View(products);
           
        }

        // Details action
        public IActionResult Details(int id)
        {
            var product = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Variants) // load variants
                .FirstOrDefault(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
