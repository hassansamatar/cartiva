namespace Cartiva.Infrastructure.Templates.Models;

public class OrderDeliveredModel
{
    public int Id { get; set; }
    public int OrderHeaderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public string? Service { get; set; }
    public string? TrackingUrl { get; set; }
    public DateTime? ShippingDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public string ShipmentStatus { get; set; } = string.Empty;
}
