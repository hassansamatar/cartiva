using Cartiva.Application.Abstractions;
using Cartiva.Domain.Interfaces;
using Cartiva.Domain.Enums;
using Cartiva.Domain;
using Cartiva.Domain.Enums;
using Cartiva.Domain.Extensions;
using Cartiva.Persistence;
using Cartiva.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace CartivaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
public class OrderController : Controller
{
    private readonly IOrderService _orderService;
    private readonly INotificationService _notificationService;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<OrderController> _logger;

    public OrderController(
        IOrderService orderService,
        INotificationService notificationService,
        ApplicationDbContext db,
        ILogger<OrderController> logger)
    {
        _orderService = orderService;
        _notificationService = notificationService;
        _db = db;
        _logger = logger;
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

        if (order.OrderStatus == Cartiva.Domain.Enums.OrderStatus.Cancelled)
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

    // POST: Admin/Order/ResendEmail/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendEmail(int id)
    {
        var order = await _db.OrderHeaders
            .Include(o => o.ApplicationUser)
            .Include(o => o.OrderDetails)
                .ThenInclude(od => od.ProductVariant)
                    .ThenInclude(pv => pv!.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
        {
            TempData["Error"] = "Order not found.";
            return RedirectToAction(nameof(Index));
        }

        if (string.IsNullOrWhiteSpace(order.ApplicationUser?.Email))
        {
            TempData["Error"] = "Cannot resend email: No customer email address found.";
            return RedirectToAction(nameof(Details), new { orderId = id });
        }

        try
        {
            // Reuse existing order confirmation notification
            var items = order.OrderDetails.Select(od => 
                $"{od.ProductVariant?.Product?.Name ?? "Product"} ({od.ProductVariant?.Color ?? "N/A"}) x {od.Count}"
            ).ToList();

            await _notificationService.SendAsync(new NotificationRequest(
                Recipient: order.ApplicationUser.Email,
                Type: NotificationType.OrderConfirmation,
                TemplateData: new Dictionary<string, object>
                {
                    ["orderId"] = order.Id.ToString(),
                    ["name"] = order.Name,
                    ["orderDate"] = order.OrderDate.ToString("dd MMM yyyy HH:mm"),
                    ["totalAmount"] = order.OrderTotal.ToString("N2", CultureInfo.GetCultureInfo("nb-NO")),
                    ["items"] = items
                },
                UserId: order.ApplicationUserId,
                ReferenceId: order.Id.ToString(),
                ReferenceType: "Order",
                Subject: $"Order Confirmation Resent - Order #{order.Id}"
            ));

            TempData["Success"] = $"Order confirmation email resent to {order.ApplicationUser.Email}.";
            _logger.LogInformation("Order {OrderId} confirmation email resent to {Email}", id, order.ApplicationUser.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resend order confirmation for Order ID {Id}", id);
            TempData["Error"] = "Failed to resend email. Please try again.";
        }

        return RedirectToAction(nameof(Details), new { orderId = id });
    }
}
