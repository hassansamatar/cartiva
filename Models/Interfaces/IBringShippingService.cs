using Models.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models.Interfaces
{
    public interface IBringShippingService
    {
        Task<BringShipmentResponse> CreateShipmentAsync(BringShipmentRequest request);
    }

    public class BringShipmentRequest
    {
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerPostalCode { get; set; }
        public string CustomerCity { get; set; }
        public string CustomerCountry { get; set; } = "NO";
        public decimal Weight { get; set; } // in kg
        public string PackageType { get; set; } // e.g., "BOX"
        public string OrderNumber { get; set; }
    }

    public class BringShipmentResponse
    {
        public bool Success { get; set; }
        public string TrackingNumber { get; set; }
        public string Carrier { get; set; } = "Bring";
        public string Service { get; set; }
        public string LabelUrl { get; set; }
        public string ErrorMessage { get; set; }
    }
}
