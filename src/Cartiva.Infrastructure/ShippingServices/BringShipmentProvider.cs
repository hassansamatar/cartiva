using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cartiva.Domain.Interfaces;
using Cartiva.Infrastructure.ShippingServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cartiva.Infrastructure.ShippingServices;

/// <summary>
/// Bring (Posten Norge) implementation of IShipmentProvider
/// Wraps existing BringShippingService with the shipment abstraction
/// </summary>
public class BringShipmentProvider : IShipmentProvider
{
    private readonly IBringShippingService _bringService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BringShipmentProvider> _logger;

    public string ProviderName => "Bring";

    public BringShipmentProvider(
        IBringShippingService bringService,
        IConfiguration configuration,
        ILogger<BringShipmentProvider> logger)
    {
        _bringService = bringService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<ShipmentCreationResult> CreateShipmentAsync(ShipmentCreationRequest request)
    {
        try
        {
            // Map to Bring-specific request
            var bringRequest = new BringShipmentRequest
            {
                OrderNumber = request.OrderNumber,
                CustomerName = request.CustomerName,
                CustomerAddress = request.CustomerAddress,
                CustomerPostalCode = request.CustomerPostalCode,
                CustomerCity = request.CustomerCity,
                CustomerCountry = request.CustomerCountry,
                CustomerPhone = request.CustomerPhone,
                Weight = request.Weight,
                PackageType = request.PackageType
            };

            var bringResponse = await _bringService.CreateShipmentAsync(bringRequest);

            if (!bringResponse.Success)
            {
                _logger.LogError("[{Provider}] Shipment creation failed: {Error}",
                    ProviderName, bringResponse.ErrorMessage);

                return new ShipmentCreationResult(
                    Success: false,
                    TrackingNumber: null,
                    CarrierReference: null,
                    LabelUrl: null,
                    EstimatedDeliveryDate: null,
                    ErrorMessage: bringResponse.ErrorMessage
                );
            }

            _logger.LogInformation("[{Provider}] Shipment created: Tracking {TrackingNumber}, Shipment ID {ShipmentId}",
                ProviderName, bringResponse.TrackingNumber, bringResponse.ShipmentId);

            return new ShipmentCreationResult(
                Success: true,
                TrackingNumber: bringResponse.TrackingNumber,
                CarrierReference: bringResponse.ShipmentId,
                LabelUrl: bringResponse.LabelUrl,
                EstimatedDeliveryDate: null // Bring doesn't provide this in creation response
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Provider}] Exception during shipment creation", ProviderName);
            return new ShipmentCreationResult(
                Success: false,
                TrackingNumber: null,
                CarrierReference: null,
                LabelUrl: null,
                EstimatedDeliveryDate: null,
                ErrorMessage: ex.Message
            );
        }
    }

    public async Task<TrackingInfoResult> GetTrackingInfoAsync(string trackingNumber)
    {
        try
        {
            var trackingData = await _bringService.GetTrackingInfoAsync(trackingNumber);

            if (trackingData == null || !trackingData.Success)
            {
                return new TrackingInfoResult(
                    TrackingNumber: trackingNumber,
                    Status: ShipmentTrackingStatus.Unknown,
                    StatusDescription: "Tracking information not available",
                    EstimatedDeliveryDate: null,
                    ActualDeliveryDate: null,
                    Events: new List<TrackingEvent>()
                );
            }

            // Map Bring status to our abstraction
            var status = MapBringStatus(trackingData.Status);

            // Map Bring events to our abstraction
            var events = trackingData.Events?.Select(e => new TrackingEvent(
                Timestamp: e.Timestamp,
                Status: e.Status,
                Description: e.Description,
                Location: e.Location
            )).ToList() ?? new List<TrackingEvent>();

            return new TrackingInfoResult(
                TrackingNumber: trackingNumber,
                Status: status,
                StatusDescription: trackingData.StatusDescription ?? "",
                EstimatedDeliveryDate: trackingData.EstimatedDeliveryDate,
                ActualDeliveryDate: trackingData.ActualDeliveryDate,
                Events: events
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Provider}] Failed to get tracking info for {TrackingNumber}",
                ProviderName, trackingNumber);

            return new TrackingInfoResult(
                TrackingNumber: trackingNumber,
                Status: ShipmentTrackingStatus.Unknown,
                StatusDescription: "Error retrieving tracking information",
                EstimatedDeliveryDate: null,
                ActualDeliveryDate: null,
                Events: new List<TrackingEvent>()
            );
        }
    }

    public async Task<ShipmentCancellationResult> CancelShipmentAsync(string shipmentId)
    {
        // Bring API doesn't expose cancellation endpoint in current implementation
        // Would need to call: DELETE /shipping/api/v1/shipments/{shipmentId}
        _logger.LogWarning("[{Provider}] Shipment cancellation not implemented", ProviderName);

        await Task.CompletedTask;
        return new ShipmentCancellationResult(
            Success: false,
            ErrorMessage: "Cancellation not implemented for Bring provider"
        );
    }

    public async Task<TrackingUpdateResult> UpdateTrackingStatusAsync(string trackingNumber)
    {
        try
        {
            var trackingInfo = await GetTrackingInfoAsync(trackingNumber);

            return new TrackingUpdateResult(
                Success: true,
                NewStatus: trackingInfo.Status
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[{Provider}] Failed to update tracking for {TrackingNumber}",
                ProviderName, trackingNumber);

            return new TrackingUpdateResult(
                Success: false,
                NewStatus: null,
                ErrorMessage: ex.Message
            );
        }
    }

    public bool IsConfigured()
    {
        var apiUid = _configuration["Bring:ApiUid"];
        var apiKey = _configuration["Bring:ApiKey"];

        return !string.IsNullOrEmpty(apiUid) && !string.IsNullOrEmpty(apiKey);
    }

    // Helper: Map Bring-specific status to our abstraction
    private ShipmentTrackingStatus MapBringStatus(string? bringStatus)
    {
        return bringStatus?.ToLower() switch
        {
            "registered" => ShipmentTrackingStatus.Created,
            "in_transit" => ShipmentTrackingStatus.InTransit,
            "out_for_delivery" => ShipmentTrackingStatus.OutForDelivery,
            "delivered" => ShipmentTrackingStatus.Delivered,
            "delivery_failed" => ShipmentTrackingStatus.DeliveryFailed,
            "returned" => ShipmentTrackingStatus.Returned,
            _ => ShipmentTrackingStatus.Unknown
        };
    }
}
