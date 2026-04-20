using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CartivaWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly IHomeService _homeService;

        public HomeController(IHomeService homeService)
        {
            _homeService = homeService;
        }

        // List all products
        public async Task<IActionResult> Index()
        {
            var products = await _homeService.GetAllProductsForBrowsingAsync();
            var activePromotions = await _homeService.GetActivePromotionsAsync();

            ViewBag.ActivePromotions = activePromotions;

            return View(products);
        }

        // Product details with variants
        public async Task<IActionResult> Details(int id)
        {
            var product = await _homeService.GetProductDetailsAsync(id);

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