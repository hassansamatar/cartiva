using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using MyUtility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class OrderHeader
    {
        // Parameterless constructor for EF Core
        public OrderHeader()
        {
        }

        // Constructor for required fields
        public OrderHeader(string applicationUserId, string name, string phoneNumber, string streetAddress, string city, string state, string postalCode)
        {
            ApplicationUserId = applicationUserId;
            Name = name;
            PhoneNumber = phoneNumber;
            StreetAddress = streetAddress;
            City = city;
            State = state;
            PostalCode = postalCode;
        }

        public int Id { get; set; }

        public string ApplicationUserId { get; set; }

        [ForeignKey("ApplicationUserId")]
        [ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; }

        public DateTime OrderDate { get; set; }

        public decimal OrderTotal { get; set; }

        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }

        public DateTime? PaymentDate { get; set; }
        public DateOnly? PaymentDueDate { get; set; }
        public string? PaymentIntentId { get; set; }

        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string StreetAddress { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

        // Helper properties
        public bool IsPending => OrderStatus == SD.StatusPending;
        public bool IsApproved => OrderStatus == SD.StatusApproved;
        public bool IsShipped => OrderStatus == SD.StatusShipped;
        public bool IsDelivered => OrderStatus == SD.StatusDelivered;
        public bool IsCancelled => OrderStatus == SD.StatusCancelled;

        public void MarkAsCancelled()
        {
            OrderStatus = SD.StatusCancelled;
            if (PaymentStatus == SD.PaymentStatusApproved)
                PaymentStatus = SD.PaymentStatusRefunded;
        }
    }
}