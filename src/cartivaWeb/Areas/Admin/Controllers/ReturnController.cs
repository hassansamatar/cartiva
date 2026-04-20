using Cartiva.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cartiva.Shared;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class ReturnController : Controller
    {
        private readonly IReturnService _returnService;

        public ReturnController(IReturnService returnService)
        {
            _returnService = returnService;
        }

        // GET: /Admin/Return/Index
        public async Task<IActionResult> Index()
        {
            var returns = await _returnService.GetAllReturnRequestsAsync();
            return View(returns);
        }

        // POST: /Admin/Return/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? adminNote)
        {
            var result = await _returnService.ApproveReturnAsync(id, adminNote);

            if (result.Success)
                TempData["success"] = result.Message;
            else
                TempData["error"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Return/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? adminNote)
        {
            var result = await _returnService.RejectReturnAsync(id, adminNote);

            if (result.Success)
                TempData["success"] = result.Message;
            else
                TempData["error"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Return/Refund/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Refund(int id)
        {
            var result = await _returnService.ProcessRefundAsync(id);

            if (result.Success)
                TempData["success"] = result.Message;
            else
                TempData["error"] = result.Message;

            return RedirectToAction(nameof(Index));
        }
    }
}
