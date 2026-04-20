using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Infrastructure.EmailServices;
using Cartiva.Infrastructure.QrCodeServices;
using Cartiva.Infrastructure.ShippingServices;
using Cartiva.Persistence;
using Cartiva.Shared;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cartiva.Application.Services;

/// <summary>
/// Service for managing shipment operations
/// </summary>
public class ShipmentService : IShipmentService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<ShipmentService> _logger;
    private readonly IBringShippingService _bringShippingService;
    private readonly IEmailSender _emailSender;
    private readonly IQrCodeService _qrCodeService;
    private readonly IEmailTemplateService _emailTemplateService;

    public ShipmentService(
        ApplicationDbContext db,
        ILogger<ShipmentService> logger,
        IBringShippingService bringShippingService,
        IEmailSender emailSender,
        IQrCodeService qrCodeService,
        IEmailTemplateService emailTemplateService)
    {
        _db = db;
        _logger = logger;
        _bringShippingService = bringShippingService;
        _emailSender = emailSender;
        _qrCodeService = qrCodeService;
        _emailTemplateService = emailTemplateService;
    }

    public async Task<List<Shipment>> GetShipmentsAsync(string? statusFilter = null)
    {
        var query = _db.Shipments
            .Include(s => s.OrderHeader)
                .ThenInclude(o => o.OrderDetails)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
            .Include(s => s.OrderHeader)
                .ThenInclude(o => o.ApplicationUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(statusFilter))
        {
            query = query.Where(s => s.ShipmentStatus == statusFilter);
        }

        return await query.OrderByDescending(s => s.Id).ToListAsync();
    }

    public async Task<Shipment?> GetShipmentByIdAsync(int id)
    {
        return await _db.Shipments
            .Include(s => s.OrderHeader)
                .ThenInclude(o => o.OrderDetails)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
            .Include(s => s.OrderHeader)
                .ThenInclude(o => o.ApplicationUser)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<ShipmentOperationResult> ApproveShipmentAsync(int shipmentId, string baseUrl)
    {
        var shipment = await _db.Shipments
            .Include(s => s.OrderHeader)
                .ThenInclude(o => o.OrderDetails)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
            return ShipmentOperationResult.Failed("Shipment not found.");

        if (shipment.ShipmentStatus != SD.ShipmentStatusPendingApproval)
            return ShipmentOperationResult.Failed("This shipment is already processed.");

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

        if (!bringResponse.Success)
        {
            _logger.LogError("Bring API error: {ErrorMessage}", bringResponse.ErrorMessage);
            return ShipmentOperationResult.Failed($"Failed to create shipment: {bringResponse.ErrorMessage}");
        }

        // Update shipment with carrier response
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

        // Send shipment confirmation email
        await SendShipmentConfirmationEmailAsync(shipment, baseUrl);

        return ShipmentOperationResult.Succeeded(
            $"Shipment approved. Tracking number: {shipment.TrackingNumber}",
            shipment.TrackingNumber,
            shipment.LabelUrl);
    }

    public async Task<ShipmentOperationResult> UpdateShipmentAsync(int shipmentId, ShipmentUpdateRequest request)
    {
        var shipment = await _db.Shipments
            .Include(s => s.OrderHeader)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
            return ShipmentOperationResult.Failed("Shipment not found.");

        // Update fields
        if (request.TrackingNumber != null)
            shipment.TrackingNumber = request.TrackingNumber;

        if (request.Carrier != null)
            shipment.Carrier = request.Carrier;

        if (request.Service != null)
            shipment.Service = request.Service;

        if (request.ShipmentStatus != null)
        {
            var oldStatus = shipment.ShipmentStatus;
            shipment.ShipmentStatus = request.ShipmentStatus;

            // Update order status and dates based on shipment status change
            if (request.ShipmentStatus == SD.ShipmentStatusShipped && 
                shipment.OrderHeader.OrderStatus != SD.StatusShipped)
            {
                shipment.OrderHeader.OrderStatus = SD.StatusShipped;
                shipment.ShippedDate = DateTime.Now;
                shipment.ShippingDate = DateTime.Now;
            }
            else if (request.ShipmentStatus == SD.ShipmentStatusDelivered && 
                     shipment.OrderHeader.OrderStatus != SD.StatusDelivered)
            {
                shipment.OrderHeader.OrderStatus = SD.StatusDelivered;
                shipment.DeliveredDate = DateTime.Now;
            }
        }

        await _db.SaveChangesAsync();

        return ShipmentOperationResult.Succeeded("Shipment updated successfully.");
    }

    public async Task<ShipmentOperationResult> CancelShipmentAsync(int shipmentId, string? reason = null)
    {
        var shipment = await _db.Shipments
            .Include(s => s.OrderHeader)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
            return ShipmentOperationResult.Failed("Shipment not found.");

        if (!await CanCancelAsync(shipmentId))
            return ShipmentOperationResult.Failed("Cannot cancel a shipment that has already been shipped.");

        shipment.ShipmentStatus = SD.ShipmentStatusCancelled;

        await _db.SaveChangesAsync();

        return ShipmentOperationResult.Succeeded("Shipment cancelled.");
    }

    public async Task<bool> CanApproveAsync(int shipmentId)
    {
        var shipment = await _db.Shipments.FindAsync(shipmentId);
        return shipment?.ShipmentStatus == SD.ShipmentStatusPendingApproval;
    }

    public async Task<bool> CanCancelAsync(int shipmentId)
    {
        var shipment = await _db.Shipments.FindAsync(shipmentId);
        if (shipment == null) return false;

        return shipment.ShipmentStatus != SD.ShipmentStatusShipped &&
               shipment.ShipmentStatus != SD.ShipmentStatusDelivered;
    }

    public async Task<Shipment> CreateShipmentForOrderAsync(int orderHeaderId)
    {
        var shipment = new Shipment
        {
            OrderHeaderId = orderHeaderId,
            ShipmentStatus = SD.ShipmentStatusPendingApproval
        };

        _db.Shipments.Add(shipment);
        await _db.SaveChangesAsync();

        return shipment;
    }

    public async Task<ShipmentOperationResult> MarkAsDeliveredAsync(int shipmentId)
    {
        var shipment = await _db.Shipments
            .Include(s => s.OrderHeader)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
            return ShipmentOperationResult.Failed("Shipment not found.");

        if (shipment.ShipmentStatus != SD.ShipmentStatusShipped)
            return ShipmentOperationResult.Failed("Shipment must be shipped before marking as delivered.");

        shipment.ShipmentStatus = SD.ShipmentStatusDelivered;
        shipment.DeliveredDate = DateTime.Now;
        shipment.OrderHeader.OrderStatus = SD.StatusDelivered;

        await _db.SaveChangesAsync();

        return ShipmentOperationResult.Succeeded("Shipment marked as delivered.");
    }

    private async Task SendShipmentConfirmationEmailAsync(Shipment shipment, string baseUrl)
    {
        var user = await _db.Users.FindAsync(shipment.OrderHeader.ApplicationUserId);
        if (user == null || string.IsNullOrEmpty(user.Email))
            return;

        var trackingUrl = $"{baseUrl}/Customer/Order/Track/{shipment.OrderHeader.Id}";
        var subject = "Your order has shipped!";

        try
        {
            if (_emailSender is EmailSender emailSender)
            {
                var qrCodeBytes = _qrCodeService.GenerateOrderQrCodeBytes(shipment.OrderHeader.Id);
                var body = await _emailTemplateService.RenderTemplateAsync("shipment-confirmation", new Dictionary<string, string>
                {
                    { "OrderId", shipment.OrderHeader.Id.ToString() },
                    { "TrackingNumber", shipment.TrackingNumber ?? "" },
                    { "TrackingUrl", trackingUrl },
                    { "QrCodeSrc", "cid:qrCode" }
                });
                await emailSender.SendEmailWithInlineImageAsync(user.Email, subject, body, qrCodeBytes);
            }
            else
            {
                var qrCodeBase64 = _qrCodeService.GenerateOrderQrCode(shipment.OrderHeader.Id);
                var body = await _emailTemplateService.RenderTemplateAsync("shipment-confirmation", new Dictionary<string, string>
                {
                    { "OrderId", shipment.OrderHeader.Id.ToString() },
                    { "TrackingNumber", shipment.TrackingNumber ?? "" },
                    { "TrackingUrl", trackingUrl },
                    { "QrCodeSrc", $"data:image/png;base64,{qrCodeBase64}" }
                });
                await _emailSender.SendEmailAsync(user.Email, subject, body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send shipment confirmation email for order {OrderId}", shipment.OrderHeader.Id);
        }
    }
}
