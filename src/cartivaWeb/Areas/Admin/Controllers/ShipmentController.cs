using Cartiva.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cartiva.Shared;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class ShipmentController : Controller
    {
        private readonly IShipmentService _shipmentService;

        public ShipmentController(IShipmentService shipmentService)
        {
            _shipmentService = shipmentService;
        }

        // GET: /Admin/Shipment/Index
        public async Task<IActionResult> Index(string? status = null)
        {
            var shipments = await _shipmentService.GetShipmentsAsync(status);
            ViewBag.CurrentStatus = status;
            return View(shipments);
        }

        // GET: /Admin/Shipment/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var shipment = await _shipmentService.GetShipmentByIdAsync(id);
            if (shipment == null)
                return NotFound();

            return View(shipment);
        }

        // GET: /Admin/Shipment/Approve/5
        [HttpGet]
        public async Task<IActionResult> Approve(int id)
        {
            var shipment = await _shipmentService.GetShipmentByIdAsync(id);
            if (shipment == null)
                return NotFound();

            if (!await _shipmentService.CanApproveAsync(id))
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
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _shipmentService.ApproveShipmentAsync(id, baseUrl);

            if (result.Success)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Shipment/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var shipment = await _shipmentService.GetShipmentByIdAsync(id);
            if (shipment == null)
                return NotFound();

            return View(shipment);
        }

        // POST: /Admin/Shipment/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string trackingNumber, string carrier, string service, string shipmentStatus)
        {
            var request = new ShipmentUpdateRequest
            {
                TrackingNumber = trackingNumber,
                Carrier = carrier,
                Service = service,
                ShipmentStatus = shipmentStatus
            };

            var result = await _shipmentService.UpdateShipmentAsync(id, request);

            if (result.Success)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Shipment/Cancel/5
        public async Task<IActionResult> Cancel(int id)
        {
            var shipment = await _shipmentService.GetShipmentByIdAsync(id);
            if (shipment == null)
                return NotFound();

            if (!await _shipmentService.CanCancelAsync(id))
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
            var result = await _shipmentService.CancelShipmentAsync(id, reason);

            if (result.Success)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmail(int id)
        {
            var result = await _shipmentService.SendShipmentEmailAsync(id);

            if (result.Success)
                TempData["Success"] = result.Message;
            else
                TempData["Error"] = result.Message;

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}