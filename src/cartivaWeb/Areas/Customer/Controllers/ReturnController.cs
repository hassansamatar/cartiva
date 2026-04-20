using Cartiva.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cartiva.Shared;
using System.Security.Claims;

namespace CartivaWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class ReturnController : Controller
    {
        private readonly IReturnService _returnService;

        public ReturnController(IReturnService returnService)
        {
            _returnService = returnService;
        }

        // GET: /Customer/Return/Create?orderDetailId=5
        [HttpGet]
        public async Task<IActionResult> Create(int orderDetailId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var validation = await _returnService.ValidateReturnRequestAsync(userId, orderDetailId);

            if (!validation.CanReturn)
            {
                TempData["error"] = validation.ErrorMessage;

                // Try to get the order header ID for redirect
                var returnRequest = await _returnService.GetReturnRequestByIdAsync(orderDetailId);
                if (returnRequest?.OrderDetail?.OrderHeaderId != null)
                {
                    return RedirectToAction("Details", "Order", new { area = "Customer", id = returnRequest.OrderDetail.OrderHeaderId });
                }
                return RedirectToAction("Index", "Order", new { area = "Customer" });
            }

            ViewBag.OrderDetail = validation.OrderDetail;
            ViewBag.DaysRemaining = validation.DaysRemaining;
            ViewBag.ReturnReasons = _returnService.GetReturnReasons();
            return View();
        }

        // POST: /Customer/Return/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int orderDetailId, string reason, string? description, int quantity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var result = await _returnService.CreateReturnRequestAsync(userId, orderDetailId, reason, description, quantity);

            if (result.Success)
            {
                TempData["success"] = result.Message;
            }
            else
            {
                TempData["error"] = result.Message;
            }

            // Get the order detail to find the order header ID for redirect
            var validation = await _returnService.ValidateReturnRequestAsync(userId, orderDetailId);
            if (validation.OrderDetail?.OrderHeaderId != null)
            {
                return RedirectToAction("Details", "Order", new { area = "Customer", id = validation.OrderDetail.OrderHeaderId });
            }

            return RedirectToAction("Index", "Order", new { area = "Customer" });
        }

        // GET: /Customer/Return/MyReturns
        [HttpGet]
        public async Task<IActionResult> MyReturns()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var returns = await _returnService.GetUserReturnRequestsAsync(userId);
            return View(returns);
        }
    }
}
