namespace Cartiva.Infrastructure.ShippingServices
{
    public interface IBringShippingService
    {
        Task<BringShipmentResponse> CreateShipmentAsync(BringShipmentRequest request);
        Task<BringTrackingResponse> GetTrackingInfoAsync(string trackingNumber);
    }

    public class BringShipmentRequest
    {
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerPostalCode { get; set; }
        public string CustomerCity { get; set; }
        public string CustomerCountry { get; set; } = "NO";
        public string CustomerPhone { get; set; }
        public decimal Weight { get; set; }
        public string PackageType { get; set; }

    }

    public class BringShipmentResponse
    {
        public bool Success { get; set; }
        public string TrackingNumber { get; set; }
        public string ShipmentId { get; set; } // Bring's internal ID
        public string Carrier { get; set; } = "Bring";
        public string Service { get; set; }
        public string LabelUrl { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class BringTrackingResponse
    {
        public bool Success { get; set; }
        public string Status { get; set; }
        public string StatusDescription { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public DateTime? ActualDeliveryDate { get; set; }
        public List<BringTrackingEvent> Events { get; set; } = new();
        public string ErrorMessage { get; set; }
    }

    public class BringTrackingEvent
    {
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
    }
}