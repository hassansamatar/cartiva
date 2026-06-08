using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cartiva.Domain.Interfaces;

/// <summary>
/// Abstraction for shipment/carrier providers (Bring, PostNord, DHL, etc.)
/// Allows pluggable shipment implementations without coupling to specific carrier
/// </summary>
public interface IShipmentProvider
{
    /// <summary>
    /// Provider/Carrier name (e.g., "Bring", "PostNord", "DHL")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Create a shipment with the carrier
    /// </summary>
    Task<ShipmentCreationResult> CreateShipmentAsync(ShipmentCreationRequest request);

    /// <summary>
    /// Get current tracking information for a shipment
    /// </summary>
    Task<TrackingInfoResult> GetTrackingInfoAsync(string trackingNumber);

    /// <summary>
    /// Cancel a shipment (if not yet picked up)
    /// </summary>
    Task<ShipmentCancellationResult> CancelShipmentAsync(string shipmentId);

    /// <summary>
    /// Update tracking status (for webhook/polling)
    /// </summary>
    Task<TrackingUpdateResult> UpdateTrackingStatusAsync(string trackingNumber);

    /// <summary>
    /// Validate carrier-specific configuration
    /// </summary>
    bool IsConfigured();
}
