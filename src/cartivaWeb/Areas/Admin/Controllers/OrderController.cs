using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Persistence;
using Cartiva.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CartivaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly ApplicationDbContext _db;

    public OrderController(IOrderService orderService, ApplicationDbContext db)
    {
        _orderService = orderService;
        _db = db;
    }

    public async Task<IActionResult> Index(string? status = null)
    {
        var orders = await _orderService.GetAllOrdersAsync(status);
        ViewBag.InvoiceByOrderId = await _db.Set<Invoice>()
            .Where(i => i.OrderHeaderId.HasValue)
            .ToDictionaryAsync(i => i.OrderHeaderId!.Value, i => i.Id);
        ViewBag.CurrentStatus = status;
        return View(orders);
    }

    public async Task<IActionResult> Details(int orderId)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return NotFound();
        }

        ViewBag.RelatedInvoice = await _db.Set<Invoice>()
            .FirstOrDefaultAsync(i => i.OrderHeaderId == orderId);

        return View(order);
    }

    [HttpGet]
    public async Task<IActionResult> Cancel(int orderId)
    {
        var order = await _orderService.GetOrderByIdAsync(orderId);
        if (order == null)
        {
            return NotFound();
        }

        if (order.OrderStatus == SD.StatusCancelled)
        {
            TempData["Error"] = "This order is already cancelled.";
            return RedirectToAction(nameof(Index));
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int orderId, string? reason)
    {
        var result = await _orderService.CancelOrderAsync(orderId, reason);

        if (result.Success)
        {
            TempData["Success"] = result.Message;
        }
        else
        {
            TempData["Error"] = result.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
