using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Infrastructure.EmailServices;
using Cartiva.Infrastructure.QrCodeServices;
using Cartiva.Persistence;
using Cartiva.Shared;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cartiva.Application.Services;

public class CompanyShipmentProcessingService : ICompanyShipmentProcessingService
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<CompanyShipmentProcessingService> _logger;
    private readonly IEmailSender _emailSender;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IConfiguration _configuration;
    private readonly IQrCodeService _qrCodeService;

    public CompanyShipmentProcessingService(
        ApplicationDbContext db,
        ILogger<CompanyShipmentProcessingService> logger,
        IEmailSender emailSender,
        IEmailTemplateService emailTemplateService,
        IConfiguration configuration,
        IQrCodeService qrCodeService)
    {
        _db = db;
        _logger = logger;
        _emailSender = emailSender;
        _emailTemplateService = emailTemplateService;
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

            var baseUrl = _configuration["AppBaseUrl"] ?? "https://localhost:7000";
            var trackingUrl = $"{baseUrl}/Order/Track/{shipment.OrderHeaderId}";

            // Generate QR code and create full data URL
            var qrCodeBase64 = _qrCodeService.GenerateOrderQrCode(shipment.OrderHeaderId);
            var qrCodeSrc = $"data:image/png;base64,{qrCodeBase64}";

            var templateData = new Dictionary<string, string>
        {
            { "OrderId", shipment.OrderHeaderId.ToString() },
            { "TrackingNumber", shipment.TrackingNumber ?? "N/A" },
            { "TrackingUrl", trackingUrl },
            { "CustomerName", user.UserName ?? user.Email },
            { "QrCodeSrc", qrCodeSrc }   // ✅ Matches your template's {{QrCodeSrc}}
        };

            var body = await _emailTemplateService.RenderTemplateAsync("shipment-confirmation", templateData);
            await _emailSender.SendEmailAsync(user.Email, "Your order has shipped", body);
            _logger.LogInformation("Shipment email sent to {Email} for Order {OrderId}", user.Email, shipment.OrderHeaderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send shipment email for Order {OrderId}", shipment.OrderHeaderId);
        }
    }

    private string GenerateTrackingNumber()
    {
        return $"BRING-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";
    }
}