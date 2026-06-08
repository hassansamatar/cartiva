using System;
using System.Collections.Generic;

namespace Cartiva.Domain.Interfaces;

/// <summary>
/// Request to create a shipment with carrier
/// </summary>
public record ShipmentCreationRequest(
    string OrderNumber,
    string CustomerName,
    string CustomerAddress,
    string CustomerPostalCode,
    string CustomerCity,
    string CustomerCountry,
    string CustomerPhone,
    string CustomerEmail,
    decimal Weight,
    string PackageType,
    Dictionary<string, string>? Metadata = null
);

/// <summary>
/// Result from creating a shipment
/// </summary>
public record ShipmentCreationResult(
    bool Success,
    string? TrackingNumber,
    string? CarrierReference,
    string? LabelUrl,
    DateTime? EstimatedDeliveryDate,
    string? ErrorMessage = null
);

/// <summary>
/// Tracking information for a shipment
/// </summary>
public record TrackingInfoResult(
    string TrackingNumber,
    ShipmentTrackingStatus Status,
    string StatusDescription,
    DateTime? EstimatedDeliveryDate,
    DateTime? ActualDeliveryDate,
    List<TrackingEvent> Events
);

/// <summary>
/// Individual tracking event
/// </summary>
public record TrackingEvent(
    DateTime Timestamp,
    string Status,
    string Description,
    string? Location
);

/// <summary>
/// Result from canceling a shipment
/// </summary>
public record ShipmentCancellationResult(
    bool Success,
    string? ErrorMessage = null
);

/// <summary>
/// Result from tracking update
/// </summary>
public record TrackingUpdateResult(
    bool Success,
    ShipmentTrackingStatus? NewStatus,
    string? ErrorMessage = null
);

/// <summary>
/// Provider-agnostic shipment tracking status
/// </summary>
public enum ShipmentTrackingStatus
{
    Created,
    LabelPrinted,
    PickedUp,
    InTransit,
    OutForDelivery,
    Delivered,
    DeliveryFailed,
    Returned,
    Canceled,
    Unknown
}
