using Cartiva.Domain;

namespace Cartiva.Application.Abstractions;

/// <summary>
/// Service for managing shipment operations
/// </summary>
public interface IShipmentService
{
    /// <summary>
    /// Get all shipments with optional status filter
    /// </summary>
    Task<List<Shipment>> GetShipmentsAsync(string? statusFilter = null);

    /// <summary>
    /// Get a shipment by ID with full order details
    /// </summary>
    Task<Shipment?> GetShipmentByIdAsync(int id);

    /// <summary>
    /// Approve a shipment and create shipping label via carrier API
    /// </summary>
    Task<ShipmentOperationResult> ApproveShipmentAsync(int shipmentId, string baseUrl);

    /// <summary>
    /// Update shipment details (tracking, carrier, status)
    /// </summary>
    Task<ShipmentOperationResult> UpdateShipmentAsync(int shipmentId, ShipmentUpdateRequest request);

    /// <summary>
    /// Cancel a shipment (only if not already shipped)
    /// </summary>
    Task<ShipmentOperationResult> CancelShipmentAsync(int shipmentId, string? reason = null);

    /// <summary>
    /// Check if a shipment can be approved
    /// </summary>
    Task<bool> CanApproveAsync(int shipmentId);

    /// <summary>
    /// Check if a shipment can be cancelled
    /// </summary>
    Task<bool> CanCancelAsync(int shipmentId);

    /// <summary>
    /// Create a shipment record for an order
    /// </summary>
    Task<Shipment> CreateShipmentForOrderAsync(int orderHeaderId);

    /// <summary>
    /// Mark shipment as delivered
    /// </summary>
    Task<ShipmentOperationResult> MarkAsDeliveredAsync(int shipmentId);

    /// <summary>
    /// Send the shipment email for an already created shipment.
    /// </summary>
    Task<ShipmentOperationResult> SendShipmentEmailAsync(int shipmentId);
}

/// <summary>
/// Result of a shipment operation
/// </summary>
public class ShipmentOperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string? LabelUrl { get; set; }

    public static ShipmentOperationResult Succeeded(string message, string? trackingNumber = null, string? labelUrl = null)
        => new() { Success = true, Message = message, TrackingNumber = trackingNumber, LabelUrl = labelUrl };

    public static ShipmentOperationResult Failed(string message)
        => new() { Success = false, Message = message };
}

/// <summary>
/// Request to update shipment details
/// </summary>
public class ShipmentUpdateRequest
{
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public string? Service { get; set; }
    public string? ShipmentStatus { get; set; }
}
