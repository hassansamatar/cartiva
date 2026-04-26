using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Domain.Extensions;
using Cartiva.Domain.Enums;
using Cartiva.Domain.Interfaces;
using Cartiva.Infrastructure.QrCodeServices;
using Cartiva.Infrastructure.ShippingServices;
using Cartiva.Persistence;
using Cartiva.Shared;
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
    private readonly IQrCodeService _qrCodeService;
    private readonly INotificationService _notificationService;

    public ShipmentService(
        ApplicationDbContext db,
        ILogger<ShipmentService> logger,
        IBringShippingService bringShippingService,
        IQrCodeService qrCodeService,
        INotificationService notificationService)
    {
        _db = db;
        _logger = logger;
        _bringShippingService = bringShippingService;
        _qrCodeService = qrCodeService;
        _notificationService = notificationService;
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
            var parsedStatus = ShipmentStatusExtensions.FromValue(statusFilter);
            query = query.Where(s => s.ShipmentStatus == parsedStatus);
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

        if (shipment.ShipmentStatus != Cartiva.Domain.Enums.ShipmentStatus.PendingApproval)
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
        shipment.ShipmentStatus = Cartiva.Domain.Enums.ShipmentStatus.Shipped;
        shipment.ShippedDate = DateTime.Now;
        shipment.ShippingDate = DateTime.Now;

        // Update order status
        shipment.OrderHeader.OrderStatus = Cartiva.Domain.Enums.OrderStatus.Shipped;

        await _db.SaveChangesAsync();

        var emailResult = await SendShipmentEmailAsync(shipment.Id);
        if (!emailResult.Success)
        {
            _logger.LogWarning("Shipment {ShipmentId} was approved but shipment email could not be queued: {Message}", shipment.Id, emailResult.Message);
        }

        return ShipmentOperationResult.Succeeded(
            $"Shipment approved. Tracking number: {shipment.TrackingNumber}",
            shipment.TrackingNumber,
            shipment.LabelUrl);
    }

    public async Task<ShipmentOperationResult> SendShipmentEmailAsync(int shipmentId)
    {
        var shipment = await _db.Shipments
            .Include(s => s.OrderHeader)
                .ThenInclude(o => o.ApplicationUser)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
            return ShipmentOperationResult.Failed("Shipment not found.");

        if (shipment.OrderHeader?.ApplicationUserId == null)
            return ShipmentOperationResult.Failed("Shipment is not linked to a customer order.");

        var user = shipment.OrderHeader.ApplicationUser ?? await _db.Users.FindAsync(shipment.OrderHeader.ApplicationUserId);
        if (string.IsNullOrWhiteSpace(user?.Email))
            return ShipmentOperationResult.Failed("Customer email address is missing.");

        if (string.IsNullOrWhiteSpace(shipment.TrackingUrl) &&
            !string.IsNullOrWhiteSpace(shipment.Carrier) &&
            !string.IsNullOrWhiteSpace(shipment.TrackingNumber))
        {
            shipment.TrackingUrl = SD.GetTrackingUrl(shipment.Carrier, shipment.TrackingNumber);
            await _db.SaveChangesAsync();
        }

        try
        {
            await _notificationService.SendAsync(new NotificationRequest(
                Recipient: user.Email,
                Type: NotificationType.OrderShipped,
                TemplateData: new Dictionary<string, object>
                {
                    ["shipmentId"] = shipment.Id.ToString(),
                    ["orderId"] = shipment.OrderHeader.Id.ToString(),
                    ["trackingNumber"] = shipment.TrackingNumber ?? "N/A",
                    ["carrier"] = shipment.Carrier ?? "Carrier",
                    ["service"] = shipment.Service ?? string.Empty,
                    ["trackingUrl"] = shipment.TrackingUrl ?? string.Empty,
                    ["shippingDate"] = shipment.ShippingDate?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty,
                    ["shippedDate"] = shipment.ShippedDate?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty,
                    ["estimatedDeliveryDate"] = shipment.ShippingDate?.AddDays(2).ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty,
                    ["shipmentStatus"] = shipment.ShipmentStatus
                },
                UserId: shipment.OrderHeader.ApplicationUserId,
                ReferenceId: shipment.Id.ToString(),
                ReferenceType: "Shipment",
                Subject: $"Your Order #{shipment.OrderHeader.Id} Has Shipped"
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to queue shipment email for shipment {ShipmentId}", shipmentId);
            return ShipmentOperationResult.Failed("Shipment email could not be sent.");
        }

        return ShipmentOperationResult.Succeeded("Shipment email sent successfully.");
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
            var parsedShipmentStatus = Cartiva.Domain.Extensions.ShipmentStatusExtensions.FromValue(request.ShipmentStatus);
            shipment.ShipmentStatus = parsedShipmentStatus;

            // Update order status and dates based on shipment status change
            if (parsedShipmentStatus == ShipmentStatus.Shipped && 
                shipment.OrderHeader.OrderStatus != Cartiva.Domain.Enums.OrderStatus.Shipped)
            {
                shipment.OrderHeader.OrderStatus = Cartiva.Domain.Enums.OrderStatus.Shipped;
                shipment.ShippedDate = DateTime.Now;
                shipment.ShippingDate = DateTime.Now;
            }
            else if (parsedShipmentStatus == ShipmentStatus.Delivered && 
                     shipment.OrderHeader.OrderStatus != Cartiva.Domain.Enums.OrderStatus.Delivered)
            {
                shipment.OrderHeader.OrderStatus = Cartiva.Domain.Enums.OrderStatus.Delivered;
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

        shipment.ShipmentStatus = Cartiva.Domain.Enums.ShipmentStatus.Cancelled;

        await _db.SaveChangesAsync();

        return ShipmentOperationResult.Succeeded("Shipment cancelled.");
    }

    public async Task<bool> CanApproveAsync(int shipmentId)
    {
        var shipment = await _db.Shipments.FindAsync(shipmentId);
        return shipment?.ShipmentStatus == Cartiva.Domain.Enums.ShipmentStatus.PendingApproval;
    }

    public async Task<bool> CanCancelAsync(int shipmentId)
    {
        var shipment = await _db.Shipments.FindAsync(shipmentId);
        if (shipment == null) return false;

        return shipment.ShipmentStatus != Cartiva.Domain.Enums.ShipmentStatus.Shipped &&
               shipment.ShipmentStatus != Cartiva.Domain.Enums.ShipmentStatus.Delivered;
    }

    public async Task<Shipment> CreateShipmentForOrderAsync(int orderHeaderId)
    {
        var shipment = new Shipment
        {
            OrderHeaderId = orderHeaderId,
            ShipmentStatus = Cartiva.Domain.Enums.ShipmentStatus.PendingApproval
        };

        _db.Shipments.Add(shipment);
        await _db.SaveChangesAsync();

        return shipment;
    }

    public async Task<ShipmentOperationResult> MarkAsDeliveredAsync(int shipmentId)
    {
        var shipment = await _db.Shipments
            .Include(s => s.OrderHeader)
                .ThenInclude(o => o.ApplicationUser)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
            return ShipmentOperationResult.Failed("Shipment not found.");

        if (shipment.ShipmentStatus != Cartiva.Domain.Enums.ShipmentStatus.Shipped)
            return ShipmentOperationResult.Failed("Shipment must be shipped before marking as delivered.");

        shipment.ShipmentStatus = Cartiva.Domain.Enums.ShipmentStatus.Delivered;
        shipment.DeliveredDate = DateTime.Now;
        shipment.OrderHeader.OrderStatus = Cartiva.Domain.Enums.OrderStatus.Delivered;

        await _db.SaveChangesAsync();

        // Send order delivered notification
        if (shipment.OrderHeader.ApplicationUser?.Email != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _notificationService.SendAsync(new NotificationRequest(
                        Recipient: shipment.OrderHeader.ApplicationUser.Email,
                        Type: NotificationType.OrderDelivered,
                        TemplateData: new Dictionary<string, object>
                        {
                            ["shipmentId"] = shipment.Id.ToString(),
                            ["orderId"] = shipment.OrderHeader.Id.ToString(),
                            ["customerName"] = string.IsNullOrWhiteSpace(shipment.OrderHeader.ApplicationUser?.Name)
                                ? shipment.OrderHeader.Name
                                : shipment.OrderHeader.ApplicationUser.Name,
                            ["trackingNumber"] = shipment.TrackingNumber ?? string.Empty,
                            ["carrier"] = shipment.Carrier ?? string.Empty,
                            ["service"] = shipment.Service ?? string.Empty,
                            ["trackingUrl"] = shipment.TrackingUrl ?? string.Empty,
                            ["shippingDate"] = shipment.ShippingDate?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty,
                            ["shippedDate"] = shipment.ShippedDate?.ToString("yyyy-MM-ddTHH:mm:ss") ?? string.Empty,
                            ["deliveredDate"] = shipment.DeliveredDate?.ToString("yyyy-MM-ddTHH:mm:ss") ?? DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                            ["shipmentStatus"] = shipment.ShipmentStatus
                        },
                        UserId: shipment.OrderHeader.ApplicationUserId,
                        ReferenceId: shipment.OrderHeader.Id.ToString(),
                        ReferenceType: "Shipment",
                        Subject: $"Your Order #{shipment.OrderHeader.Id} Has Been Delivered!"
                    ));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send order delivered notification for shipment {ShipmentId}", shipmentId);
                }
            });
        }

        return ShipmentOperationResult.Succeeded("Shipment marked as delivered.");
    }
}
