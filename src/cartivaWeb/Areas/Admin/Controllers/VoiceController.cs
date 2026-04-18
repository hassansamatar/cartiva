using Microsoft.AspNetCore.Mvc;

namespace cartivaWeb.Areas.Admin.Controllers
{
    public class VoiceController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
