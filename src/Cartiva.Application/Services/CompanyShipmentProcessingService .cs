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
    private readonly IShipmentService _shipmentService;
    private readonly IInvoiceService _invoiceService;

    public CompanyShipmentProcessingService(
        ApplicationDbContext db,
        ILogger<CompanyShipmentProcessingService> logger,
        IConfiguration configuration,
        IQrCodeService qrCodeService,
        IShipmentService shipmentService,
        IInvoiceService invoiceService)
    {
        _db = db;
        _logger = logger;
        _configuration = configuration;
        _qrCodeService = qrCodeService;
        _shipmentService = shipmentService;
        _invoiceService = invoiceService;
    }

    public async Task<int> ProcessApprovedShipmentsAsync(CancellationToken ct)
    {
        var shipments = await _db.Shipments
            .Include(s => s.OrderHeader)
                .ThenInclude(o => o.ApplicationUser)
                    .ThenInclude(u => u.Company)
            .Where(s => s.ShipmentStatus == SD.ShipmentStatusApproved &&
                        s.OrderHeader.OrderStatus != SD.StatusShipped &&
                        s.OrderHeader.PaymentStatus == SD.PaymentStatusDeferred &&
                        s.OrderHeader.ApplicationUser.CompanyId != null &&
                        s.OrderHeader.ApplicationUser.Company != null &&
                        s.OrderHeader.ApplicationUser.Company.IsActive)
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
            shipment.Carrier ??= SD.CarrierBring;
            shipment.TrackingUrl = SD.GetTrackingUrl(shipment.Carrier, shipment.TrackingNumber);
            shipment.ShippingDate = DateTime.UtcNow;
            shipment.ShippedDate = DateTime.UtcNow;
            shipment.ShipmentStatus = SD.ShipmentStatusShipped;
            shipment.OrderHeader.OrderStatus = SD.StatusShipped;
            _logger.LogInformation("Shipped OrderHeaderId {OrderId} with tracking {Tracking}",
                shipment.OrderHeaderId, shipment.TrackingNumber);
        }

        await _db.SaveChangesAsync(ct);

        foreach (var shipment in shipments)
        {
            var shipmentEmailResult = await _shipmentService.SendShipmentEmailAsync(shipment.Id);
            if (!shipmentEmailResult.Success)
            {
                _logger.LogWarning("Automatic shipment email failed for shipment {ShipmentId}: {Message}", shipment.Id, shipmentEmailResult.Message);
            }

            var invoice = await _invoiceService.GetInvoiceByOrderIdAsync(shipment.OrderHeaderId)
                ?? await _invoiceService.GenerateInvoiceFromOrderAsync(shipment.OrderHeaderId, ct);

            var invoiceSendResult = await _invoiceService.SendInvoiceAsync(invoice.Id, ct);
            if (!invoiceSendResult)
            {
                _logger.LogWarning("Automatic invoice send failed for invoice {InvoiceId} linked to order {OrderId}", invoice.Id, shipment.OrderHeaderId);
            }
        }

        _logger.LogInformation("Processed {Count} active-company deferred shipments with shipment and invoice emails.", shipments.Count);
        return shipments.Count;
    }

    private string GenerateTrackingNumber()
    {
        return $"BRING-{DateTime.UtcNow:yyyyMMddHHmmss}-{new Random().Next(1000, 9999)}";
    }
}