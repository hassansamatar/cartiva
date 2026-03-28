using DataAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
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
        private readonly IBringShippingService _bringShippingService;
        private readonly IEmailSender _emailSender;
        private readonly IQrCodeService _qrCodeService;

        public ShipmentController(ApplicationDbContext db,
                                  ILogger<ShipmentController> logger,
                                  IBringShippingService bringShippingService,
                                  IEmailSender emailSender,
                                  IQrCodeService qrCodeService)
        {
            _db = db;
            _logger = logger;
            _bringShippingService = bringShippingService;
            _emailSender = emailSender;
            _qrCodeService = qrCodeService;
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
                query = query.Where(s => s.ShipmentStatus == status);

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
        [HttpGet]
        public async Task<IActionResult> Approve(int id)
        {
            var shipment = await _db.Shipments
                .Include(s => s.OrderHeader)
                    .ThenInclude(o => o.OrderDetails)
                        .ThenInclude(d => d.ProductVariant)
                            .ThenInclude(v => v.Product)
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

        // POST: /Admin/Shipment/ApprovePost
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApprovePost(int id)
        {
            var shipment = await _db.Shipments
                .Include(s => s.OrderHeader)
                    .ThenInclude(o => o.OrderDetails)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null)
                return NotFound();

            if (shipment.ShipmentStatus != SD.ShipmentStatusPendingApproval)
            {
                TempData["Error"] = "This shipment is already processed.";
                return RedirectToAction(nameof(Index));
            }

            // Prepare request to shipping service
            var request = new BringShipmentRequest
            {
                OrderNumber = shipment.OrderHeader.Id.ToString(),
                CustomerName = shipment.OrderHeader.Name,
                CustomerAddress = shipment.OrderHeader.StreetAddress,
                CustomerPostalCode = shipment.OrderHeader.PostalCode,
                CustomerCity = shipment.OrderHeader.City,
                CustomerCountry = shipment.OrderHeader.Country ?? "NO",
                CustomerPhone = shipment.OrderHeader.PhoneNumber,
                Weight = 1.0m, // TODO: calculate total weight from order items
                PackageType = "BOX"
            };

            _logger.LogInformation("Creating shipment for order {OrderId}", shipment.OrderHeader.Id);
            var bringResponse = await _bringShippingService.CreateShipmentAsync(request);

            if (bringResponse.Success)
            {
                shipment.TrackingNumber = bringResponse.TrackingNumber;
                shipment.Carrier = bringResponse.Carrier;
                shipment.Service = bringResponse.Service;
                shipment.LabelUrl = bringResponse.LabelUrl;
                shipment.ShipmentStatus = SD.ShipmentStatusShipped;
                shipment.ShippedDate = DateTime.Now;
                shipment.ShippingDate = DateTime.Now;

                shipment.OrderHeader.OrderStatus = SD.StatusShipped;

                await _db.SaveChangesAsync();

                // Send shipment confirmation email with inline QR code
                var user = await _db.Users.FindAsync(shipment.OrderHeader.ApplicationUserId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    var trackingUrl = Url.Action("Track", "Order", new { id = shipment.OrderHeader.Id, area = "Customer" }, Request.Scheme);
                    var qrCodeBytes = _qrCodeService.GenerateOrderQrCodeBytes(shipment.OrderHeader.Id);
                    var subject = "Your order has shipped!";
                    var body = $@"
<h2>Good news!</h2>
<p>Your order <strong>#{shipment.OrderHeader.Id}</strong> has been shipped.</p>
<p><strong>Tracking number:</strong> {shipment.TrackingNumber}</p>
<p>Track your order: <a href='{trackingUrl}'>Click here</a></p>
<p>Or scan the QR code below with your phone:</p>
<img src='cid:qrCode' width='150' />
<p>Thank you for shopping with us!</p>
";

                    if (_emailSender is MyUtility.EmailSender emailSender)
                    {
                        await emailSender.SendEmailWithInlineImageAsync(user.Email, subject, body, qrCodeBytes);
                    }
                    else
                    {
                        // Fallback to base64 image
                        var qrCodeBase64 = _qrCodeService.GenerateOrderQrCode(shipment.OrderHeader.Id);
                        var fallbackBody = $@"
<h2>Good news!</h2>
<p>Your order <strong>#{shipment.OrderHeader.Id}</strong> has been shipped.</p>
<p><strong>Tracking number:</strong> {shipment.TrackingNumber}</p>
<p>Track your order: <a href='{trackingUrl}'>Click here</a></p>
<p>Or scan the QR code below with your phone:</p>
<img src='data:image/png;base64,{qrCodeBase64}' width='150' />
<p>Thank you for shopping with us!</p>
";
                        await _emailSender.SendEmailAsync(user.Email, subject, fallbackBody);
                    }
                }

                TempData["Success"] = $"Shipment approved. Tracking number: {shipment.TrackingNumber}";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                _logger.LogError("Bring API error: {ErrorMessage}", bringResponse.ErrorMessage);
                TempData["Error"] = $"Failed to create shipment: {bringResponse.ErrorMessage}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Admin/Shipment/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var shipment = await _db.Shipments
                .Include(s => s.OrderHeader)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (shipment == null)
                return NotFound();

            return View(shipment);
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

            await _db.SaveChangesAsync();

            TempData["Success"] = "Shipment cancelled.";
            return RedirectToAction(nameof(Index));
        }
    }
}