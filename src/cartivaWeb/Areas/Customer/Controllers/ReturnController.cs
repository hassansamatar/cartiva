using Cartiva.Application.Abstractions;
using Cartiva.Domain.ViewModels;
using Cartiva.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        [HttpGet]
        // GET: Create return
        [HttpGet]
        public async Task<IActionResult> Create(int orderDetailId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var validation = await _returnService.ValidateReturnRequestAsync(userId, orderDetailId);

            if (!validation.CanReturn)
            {
                TempData["error"] = validation.ErrorMessage;

                return RedirectToAction("Details", "Order",
                    new { area = "Customer", id = validation.OrderDetail?.OrderHeaderId });
            }

            var vm = new ReturnVm
            {
                OrderDetailId = orderDetailId,
                OrderDetail = validation.OrderDetail!,
                DaysRemaining = validation.DaysRemaining,
                Reasons = SD.GetReturnReasons()
                    .Select(r => new SelectListItem
                    {
                        Text = r,
                        Value = r
                    })
            };

            return View(vm);
        }

        // POST: Create return
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReturnVm vm, string reason, string? description, int quantity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // basic safety check
            if (vm.OrderDetailId <= 0)
            {
                TempData["error"] = "Invalid order item.";
                return RedirectToAction("Index", "Order", new { area = "Customer" });
            }

            var result = await _returnService.CreateReturnRequestAsync(
                userId,
                vm.OrderDetailId,
                reason,
                description,
                quantity
            );

            TempData[result.Success ? "success" : "error"] = result.Message;

            // get order header for redirect
            var validation = await _returnService.ValidateReturnRequestAsync(userId, vm.OrderDetailId);

            if (validation.OrderDetail?.OrderHeaderId != null)
            {
                return RedirectToAction("Details", "Order", new
                {
                    area = "Customer",
                    id = validation.OrderDetail.OrderHeaderId
                });
            }

            return RedirectToAction("History", "Order", new { area = "Customer" });
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