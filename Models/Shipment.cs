using MyUtility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Models
{
    public class Shipment
    {
        public int Id { get; set; }

        public int OrderHeaderId { get; set; }
        [ForeignKey("OrderHeaderId")]
        public OrderHeader OrderHeader { get; set; }

        // Tracking information
        public string? TrackingNumber { get; set; }
        public string? Carrier { get; set; }
        public string? Service { get; set; }
        public string? TrackingUrl { get; set; }

        // Shipment timeline
        public DateTime? ShippingDate { get; set; }
        public DateTime? ShippedDate { get; set; }
        public DateTime? DeliveredDate { get; set; }

        // Status
        public string ShipmentStatus { get; set; } = SD.ShipmentStatusPendingApproval;

        // Shipping label (optional)
        public string? LabelUrl { get; set; }

        // Additional carrier data (JSON)
        public string? CarrierData { get; set; }

        // Package details
        public decimal? Weight { get; set; }       // in kg
        public string? PackageType { get; set; }   // e.g., "Box", "Envelope"
    }
}
