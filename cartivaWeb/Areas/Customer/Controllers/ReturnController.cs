using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using ApplicationUtility;
using System.Security.Claims;

namespace CartivaWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class ReturnController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ReturnController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET: /Customer/Return/Create?orderDetailId=5
        [HttpGet]
        public async Task<IActionResult> Create(int orderDetailId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var orderDetail = await _db.OrderDetails
                .Include(od => od.OrderHeader)
                .Include(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .Include(od => od.ProductVariant)
                    .ThenInclude(pv => pv.SizeValue)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId
                    && od.OrderHeader.ApplicationUserId == userId);

            if (orderDetail == null)
                return NotFound();

            // Must be delivered
            if (orderDetail.OrderHeader.OrderStatus != SD.StatusDelivered)
            {
                TempData["error"] = "Returns can only be requested for delivered orders.";
                return RedirectToAction("Details", "Order", new { area = "Customer", id = orderDetail.OrderHeaderId });
            }

            // Check return window
            var deliveredDate = orderDetail.OrderHeader.OrderDate; // use order date as fallback
            var shipment = await _db.Shipments
                .FirstOrDefaultAsync(s => s.OrderHeaderId == orderDetail.OrderHeaderId && s.DeliveredDate != null);
            if (shipment?.DeliveredDate != null)
                deliveredDate = shipment.DeliveredDate.Value;

            var daysSinceDelivery = (DateTime.UtcNow - deliveredDate).Days;
            if (daysSinceDelivery > SD.ReturnWindowDays)
            {
                TempData["error"] = $"The {SD.ReturnWindowDays}-day return window has expired.";
                return RedirectToAction("Details", "Order", new { area = "Customer", id = orderDetail.OrderHeaderId });
            }

            // Check if already has a pending/approved return
            var existingReturn = await _db.ReturnRequests
                .AnyAsync(r => r.OrderDetailId == orderDetailId
                    && (r.Status == SD.ReturnStatusPending || r.Status == SD.ReturnStatusApproved || r.Status == SD.ReturnStatusRefunded));

            if (existingReturn)
            {
                TempData["error"] = "A return request already exists for this item.";
                return RedirectToAction("Details", "Order", new { area = "Customer", id = orderDetail.OrderHeaderId });
            }

            ViewBag.OrderDetail = orderDetail;
            ViewBag.DaysRemaining = SD.ReturnWindowDays - daysSinceDelivery;
            ViewBag.ReturnReasons = SD.GetReturnReasons();
            return View();
        }

        // POST: /Customer/Return/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int orderDetailId, string reason, string? description, int quantity)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var orderDetail = await _db.OrderDetails
                .Include(od => od.OrderHeader)
                .FirstOrDefaultAsync(od => od.Id == orderDetailId
                    && od.OrderHeader.ApplicationUserId == userId);

            if (orderDetail == null)
                return NotFound();

            if (orderDetail.OrderHeader.OrderStatus != SD.StatusDelivered)
            {
                TempData["error"] = "Returns can only be requested for delivered orders.";
                return RedirectToAction("Details", "Order", new { area = "Customer", id = orderDetail.OrderHeaderId });
            }

            if (quantity < 1 || quantity > orderDetail.Count)
            {
                TempData["error"] = $"Quantity must be between 1 and {orderDetail.Count}.";
                return RedirectToAction("Create", new { orderDetailId });
            }

            var returnRequest = new ReturnRequest
            {
                OrderDetailId = orderDetailId,
                ApplicationUserId = userId,
                Reason = reason,
                Description = description?.Trim(),
                Quantity = quantity,
                RequestDate = DateTime.UtcNow,
                Status = SD.ReturnStatusPending,
                RefundAmount = orderDetail.Price * quantity
            };

            _db.ReturnRequests.Add(returnRequest);
            await _db.SaveChangesAsync();

            TempData["success"] = "Return request submitted. We will review it shortly.";
            return RedirectToAction("Details", "Order", new { area = "Customer", id = orderDetail.OrderHeaderId });
        }

        // GET: /Customer/Return/MyReturns
        [HttpGet]
        public async Task<IActionResult> MyReturns()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var returns = await _db.ReturnRequests
                .Where(r => r.ApplicationUserId == userId)
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.OrderHeader)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(returns);
        }
    }
}
