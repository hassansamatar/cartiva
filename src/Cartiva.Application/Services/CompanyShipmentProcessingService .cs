using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Infrastructure.QrCodeServices;
using Cartiva.Persistence;
using Cartiva.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cartiva.Application.Services;

public class CompanyShipmentProcessingService : ICompanyShipmentProcessingService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CompanyShipmentProcessingService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IQrCodeService _qrCodeService;

    public CompanyShipmentProcessingService(
        ApplicationDbContext db,
        ILogger<CompanyShipmentProcessingService> logger,
        IConfiguration configuration,
        IQrCodeService qrCodeService)
    {
        _db = db;
        _logger = logger;
        _configuration = configuration;
        _qrCodeService = qrCodeService;
    }

    public async Task<int> ProcessApprovedShipmentsAsync(CancellationToken ct)
    {
        var shipments = await _db.Shipments
            .Include(s => s.OrderHeader)
                .ThenInclude(o => o.ApplicationUser)
            .Where(s => s.ShipmentStatus == SD.ShipmentStatusApproved &&
                        s.OrderHeader.OrderStatus != SD.StatusShipped)
            .Take(50)
            .ToListAsync(ct);

        if (!shipments.Any())
        {
            _logger.LogInformation("No approved shipments waiting to be shipped.");
            return 0;
        }

        foreach (var shipment in shipments)
        {
            shipment.TrackingNumber = GenerateTrackingNumber();
            shipment.ShipmentStatus = SD.ShipmentStatusShipped;
            shipment.OrderHeader.OrderStatus = SD.StatusShipped;
            _logger.LogInformation("Shipped OrderHeaderId {OrderId} with tracking {Tracking}",
                shipment.OrderHeaderId, shipment.TrackingNumber);
        }

        await _db.SaveChangesAsync(ct);

        foreach (var shipment in shipments)
        {
            await SendShipmentEmailAsync(shipment);
        }

        _logger.LogInformation("Processed {Count} shipments as shipped and sent emails.", shipments.Count);
        return shipments.Count;
    }

    private async Task SendShipmentEmailAsync(Shipment shipment)
    {
        try
        {
            var user = shipment.OrderHeader?.ApplicationUser;
            if (user == null || string.IsNullOrEmpty(user.Email))
            {
                _logger.LogWarning("No user email for Order {OrderId}", shipment.OrderHeaderId);
                return;
            }

            // NOTE: Email notifications are now handled by ShipmentService via the notification system
            // This legacy email sending code has been removed to avoid duplicate notifications

            _logger.LogInformation("Shipment processed for Order {OrderId}. Email notification handled by ShipmentService.", 
                shipment.OrderHeaderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process shipment for Order {OrderId}", shipment.OrderHeaderId);
        }
    }

    private string GenerateTrackingNumber()
    {
        return $"BRING-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";
    }
}