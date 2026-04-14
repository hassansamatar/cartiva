using Cartiva.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cartiva.Domain;
using System.Diagnostics;
using Cartiva.Persistence;

namespace CartivaWeb.Areas.Customer.Controllers
{
   [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly Cartiva.Persistence.ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }

        // List all products
        public async Task<IActionResult> Index()
        {
            var products = await _db.Products
                .Include(p => p.Category)
                    .ThenInclude(c => c.DefaultSizeSystem)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.SizeValue)
                        .ThenInclude(sv => sv.SizeSystem)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Reviews!.Where(r => r.IsApproved))
                .AsNoTracking()
                .ToListAsync();

            var activePromotions = await _db.Promotions
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StartDate <= DateTime.Now && p.EndDate >= DateTime.Now)
                .AsNoTracking()
                .ToListAsync();

            ViewBag.ActivePromotions = activePromotions;

            return View(products);
        }

        // Product details with variants
        public async Task<IActionResult> Details(int id)
        {
            var product = await _db.Products
                .Include(p => p.Category)
                    .ThenInclude(c => c.DefaultSizeSystem)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.SizeValue)
                        .ThenInclude(sv => sv.SizeSystem)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.Reviews!.Where(r => r.IsApproved))
                        .ThenInclude(r => r.ApplicationUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
                return NotFound();

            return View(product);
        }

        // Privacy page
        public IActionResult Privacy()
        {
            return View();
        }

        // Error page
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}