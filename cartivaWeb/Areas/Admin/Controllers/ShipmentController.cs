using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Interfaces;
using MyUtility;
using System.Linq;
using System.Threading.Tasks;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class ShipmentController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ShipmentController> _logger;
        // TODO: Inject IEmailSender when ready
        private readonly IBringShippingService _bringShippingService;
        public ShipmentController(ApplicationDbContext db, ILogger<ShipmentController> logger, IBringShippingService bringShippingService)
        {
            _db = db;
            _logger = logger;
            _bringShippingService = bringShippingService;
        }

        // GET: /Admin/Shipment/Index
        public async Task<IActionResult> Index(string status = null)
        {
            var query = _db.Shipments
                .Include(s => s.OrderHeader)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(d => d.ProductVariant)
                            .ThenInclude(v => v.Product)
                .Include(s => s.OrderHeader)
                    .ThenInclude(o => o.ApplicationUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(s => s.ShipmentStatus == status);
            }

            var shipments = await query.OrderByDescending(s => s.Id).ToListAsync();

            ViewBag.CurrentStatus = status;
            return View(shipments);
        }

        // GET: /Admin/Shipment/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var shipment = await _db.Shipments
                .Include(s => s.OrderHeader)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(d => d.ProductVariant)
                            .ThenInclude(v => v.Product)
                .Include(s => s.OrderHeader)
                    .ThenInclude(o => o.ApplicationUser)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null)
                return NotFound();

            return View(shipment);
        }

        // GET: /Admin/Shipment/Approve/5
        public async Task<IActionResult> Approve(int id)
        {
            var shipment = await _db.Shipments
                .Include(s => s.OrderHeader)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null)
                return NotFound();

            if (shipment.ShipmentStatus != SD.ShipmentStatusPendingApproval)
            {
                TempData["Error"] = "This shipment is already processed.";
                return RedirectToAction(nameof(Index));
            }

            return View(shipment);
        }

        // POST: /Admin/Shipment/Approve
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string trackingNumber, string carrier, string service)
        {
            var shipment = await _db.Shipments
                .Include(s => s.OrderHeader)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null)
                return NotFound();

            if (shipment.ShipmentStatus != SD.ShipmentStatusPendingApproval)
            {
                TempData["Error"] = "This shipment is already processed.";
                return RedirectToAction(nameof(Index));
            }

            // Prepare request to Bring API
            var request = new BringShipmentRequest
            {
                CustomerName = shipment.OrderHeader.Name,
                CustomerAddress = shipment.OrderHeader.StreetAddress,
                CustomerPostalCode = shipment.OrderHeader.PostalCode,
                CustomerCity = shipment.OrderHeader.City,
                CustomerCountry = "NO",
                Weight = 1.0m, // TODO: calculate based on product variants
                PackageType = "BOX",
                OrderNumber = shipment.OrderHeader.Id.ToString()
            };

            var bringResponse = await _bringShippingService.CreateShipmentAsync(request);

            if (bringResponse.Success)
            {
                // Update shipment with returned data
                shipment.TrackingNumber = bringResponse.TrackingNumber;
                shipment.Carrier = bringResponse.Carrier;
                shipment.Service = bringResponse.Service;
                shipment.LabelUrl = bringResponse.LabelUrl;
                shipment.ShipmentStatus = SD.ShipmentStatusShipped;
                shipment.ShippedDate = DateTime.Now;
                shipment.ShippingDate = DateTime.Now;

                // Update order status
                shipment.OrderHeader.OrderStatus = SD.StatusShipped;

                await _db.SaveChangesAsync();

                // TODO: Send email to customer with tracking link and QR code

                TempData["Success"] = $"Shipment approved. Tracking number: {shipment.TrackingNumber}";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                TempData["Error"] = $"Failed to create shipment with Bring: {bringResponse.ErrorMessage}";
                return RedirectToAction(nameof(Index));
            }
        }

            // POST: /Admin/Shipment/Edit
            [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string trackingNumber, string carrier, string service, string shipmentStatus)
        {
            var shipment = await _db.Shipments
                .Include(s => s.OrderHeader)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null)
                return NotFound();

            shipment.TrackingNumber = trackingNumber;
            shipment.Carrier = carrier;
            shipment.Service = service;
            shipment.ShipmentStatus = shipmentStatus;

            // If status is changed to Shipped, also update order status and shipment dates
            if (shipmentStatus == SD.ShipmentStatusShipped && shipment.OrderHeader.OrderStatus != SD.StatusShipped)
            {
                shipment.OrderHeader.OrderStatus = SD.StatusShipped;
                shipment.ShippedDate = DateTime.Now;
                shipment.ShippingDate = DateTime.Now;
            }
            else if (shipmentStatus == SD.ShipmentStatusDelivered && shipment.OrderHeader.OrderStatus != SD.StatusDelivered)
            {
                shipment.OrderHeader.OrderStatus = SD.StatusDelivered;
                shipment.DeliveredDate = DateTime.Now;
            }

            await _db.SaveChangesAsync();

            TempData["Success"] = "Shipment updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Shipment/Cancel/5
        public async Task<IActionResult> Cancel(int id)
        {
            var shipment = await _db.Shipments
                .Include(s => s.OrderHeader)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null)
                return NotFound();

            if (shipment.ShipmentStatus == SD.ShipmentStatusShipped || shipment.ShipmentStatus == SD.ShipmentStatusDelivered)
            {
                TempData["Error"] = "Cannot cancel a shipment that has already been shipped.";
                return RedirectToAction(nameof(Index));
            }

            return View(shipment);
        }

        // POST: /Admin/Shipment/Cancel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            var shipment = await _db.Shipments
                .Include(s => s.OrderHeader)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null)
                return NotFound();

            if (shipment.ShipmentStatus == SD.ShipmentStatusShipped || shipment.ShipmentStatus == SD.ShipmentStatusDelivered)
            {
                TempData["Error"] = "Cannot cancel a shipment that has already been shipped.";
                return RedirectToAction(nameof(Index));
            }

            shipment.ShipmentStatus = SD.ShipmentStatusCancelled;
            // Optionally, also update order status to Cancelled? Usually order would be cancelled entirely, not just shipment.
            // For now, we'll leave order status as is, but admin may need to handle refund separately.

            await _db.SaveChangesAsync();

            TempData["Success"] = "Shipment cancelled.";
            return RedirectToAction(nameof(Index));
        }
    }
}