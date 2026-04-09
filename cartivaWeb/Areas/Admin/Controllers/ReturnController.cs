using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using ApplicationUtility;
using Stripe;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class ReturnController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ReturnController> _logger;

        public ReturnController(ApplicationDbContext db, ILogger<ReturnController> logger)
        {
            _db = db;
            _logger = logger;
        }

        // GET: /Admin/Return/Index
        public async Task<IActionResult> Index()
        {
            var returns = await _db.ReturnRequests
                .Include(r => r.ApplicationUser)
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.OrderHeader)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();

            return View(returns);
        }

        // POST: /Admin/Return/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? adminNote)
        {
            var returnRequest = await _db.ReturnRequests
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.OrderHeader)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (returnRequest == null) return NotFound();

            returnRequest.Status = SD.ReturnStatusApproved;
            returnRequest.AdminNote = adminNote;
            returnRequest.ResolvedDate = DateTime.UtcNow;

            // Restore stock
            var variant = await _db.ProductVariants.FindAsync(returnRequest.OrderDetail.ProductVariantId);
            if (variant != null)
            {
                variant.Stock += returnRequest.Quantity;
            }

            await _db.SaveChangesAsync();

            TempData["success"] = "Return approved. Stock restored. You can now process the refund.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Return/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? adminNote)
        {
            var returnRequest = await _db.ReturnRequests.FindAsync(id);
            if (returnRequest == null) return NotFound();

            returnRequest.Status = SD.ReturnStatusRejected;
            returnRequest.AdminNote = adminNote;
            returnRequest.ResolvedDate = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            TempData["success"] = "Return request rejected.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Admin/Return/Refund/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Refund(int id)
        {
            var returnRequest = await _db.ReturnRequests
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.OrderHeader)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (returnRequest == null) return NotFound();

            if (returnRequest.Status != SD.ReturnStatusApproved)
            {
                TempData["error"] = "Only approved returns can be refunded.";
                return RedirectToAction(nameof(Index));
            }

            var order = returnRequest.OrderDetail.OrderHeader;
            var refundAmount = returnRequest.RefundAmount ?? (returnRequest.OrderDetail.Price * returnRequest.Quantity);

            // Process Stripe refund if payment was made via Stripe
            if (!string.IsNullOrEmpty(order.PaymentIntentId) &&
                order.PaymentStatus == SD.PaymentStatusApproved)
            {
                try
                {
                    var options = new RefundCreateOptions
                    {
                        PaymentIntent = order.PaymentIntentId,
                        Amount = (long)(refundAmount * 100) // partial refund in øre
                    };
                    var service = new RefundService();
                    var refund = await service.CreateAsync(options);

                    if (refund.Status == "succeeded" || refund.Status == "pending")
                    {
                        returnRequest.RefundId = refund.Id;
                        _logger.LogInformation("Stripe refund {RefundId} for return {ReturnId}, amount {Amount}",
                            refund.Id, id, refundAmount);
                    }
                    else
                    {
                        TempData["error"] = $"Stripe refund status: {refund.Status}. Please try again.";
                        return RedirectToAction(nameof(Index));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Stripe refund failed for return {ReturnId}", id);
                    TempData["error"] = "Refund failed: " + ex.Message;
                    return RedirectToAction(nameof(Index));
                }
            }

            returnRequest.Status = SD.ReturnStatusRefunded;
            returnRequest.RefundDate = DateTime.UtcNow;
            returnRequest.RefundAmount = refundAmount;

            // Check if all items are returned/refunded — update order status
            var allOrderDetails = await _db.OrderDetails
                .Where(od => od.OrderHeaderId == order.Id)
                .ToListAsync();

            var allDetailIds = allOrderDetails.Select(od => od.Id).ToList();
            var allReturns = await _db.ReturnRequests
                .Where(r => allDetailIds.Contains(r.OrderDetailId) && r.Status == SD.ReturnStatusRefunded)
                .ToListAsync();

            var totalOrderedQty = allOrderDetails.Sum(od => od.Count);
            var totalRefundedQty = allReturns.Sum(r => r.Quantity);

            if (totalRefundedQty >= totalOrderedQty)
            {
                order.OrderStatus = SD.StatusRefunded;
                order.PaymentStatus = SD.PaymentStatusRefunded;
            }

            await _db.SaveChangesAsync();

            TempData["success"] = $"Refund of {refundAmount:C} processed successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
