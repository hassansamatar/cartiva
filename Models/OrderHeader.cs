using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using MyUtility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Models
{
    public class OrderHeader
    {
        public int Id { get; set; }

        public string ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        [ValidateNever]
        public ApplicationUser ApplicationUser { get; set; }

        public DateTime OrderDate { get; set; }

        public DateTime? ShippingDate { get; set; }

        public DateTime? ShippedDate { get; set; }          // ← ADD THIS - When order was actually shipped

        public DateTime? DeliveredDate { get; set; }        // ← ADD THIS - When order was delivered

        public decimal OrderTotal { get; set; }

        public string? OrderStatus { get; set; }

        public string? PaymentStatus { get; set; }

        public string? TrackingNumber { get; set; }

        public string? Carrier { get; set; }

        public DateTime? PaymentDate { get; set; }

        public DateOnly? PaymentDueDate { get; set; }

        public string? PaymentIntentId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string PhoneNumber { get; set; }

        [Required]
        public string StreetAddress { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string State { get; set; }

        [Required]
        public string PostalCode { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; }
        // Add these properties/methods to OrderHeader class

        public bool IsPending => OrderStatus == SD.StatusPending;
        public bool IsApproved => OrderStatus == SD.StatusApproved;
        public bool IsShipped => OrderStatus == SD.StatusShipped;
        public bool IsDelivered => OrderStatus == SD.StatusDelivered;
        public bool IsCancelled => OrderStatus == SD.StatusCancelled;

        // Method to update order status
        public void MarkAsShipped(string trackingNumber = null, string carrier = null)
        {
            OrderStatus = SD.StatusShipped;
            ShippedDate = DateTime.Now;
            ShippingDate = DateTime.Now;

            if (!string.IsNullOrEmpty(trackingNumber))
                TrackingNumber = trackingNumber;

            if (!string.IsNullOrEmpty(carrier))
                Carrier = carrier;
        }

        public void MarkAsDelivered()
        {
            OrderStatus = SD.StatusDelivered;
            DeliveredDate = DateTime.Now;
        }

        public void MarkAsCancelled()
        {
            OrderStatus = SD.StatusCancelled;
            if (PaymentStatus == SD.PaymentStatusApproved)
                PaymentStatus = SD.PaymentStatusRefunded;
        }
    }
}