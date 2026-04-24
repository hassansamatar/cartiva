using Cartiva.Domain.Enums;
using Cartiva.Infrastructure.Notifications.Interfaces;
using Cartiva.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace cartivaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class NotificationTestController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly INotificationQueue _queue;

    public NotificationTestController(ApplicationDbContext db, INotificationQueue queue)
    {
        _db = db;
        _queue = queue;
    }

    // GET: /Admin/NotificationTest/ProcessPending
    public async Task<IActionResult> ProcessPending()
    {
        var pendingNotifications = await _db.Notifications
            .Where(n => n.Status == NotificationStatus.Pending)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync();

        foreach (var notification in pendingNotifications)
        {
            await _queue.EnqueueAsync(notification.Id);
        }

        TempData["Success"] = $"Enqueued {pendingNotifications.Count} pending notifications for processing.";
        return RedirectToAction("Index");
    }

    // GET: /Admin/NotificationTest/Index
    public async Task<IActionResult> Index()
    {
        var notifications = await _db.Notifications
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        return View(notifications);
    }
}
