using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using ApplicationUtility;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public OrderHeader(string applicationUserId, string name, string phoneNumber, string streetAddress, string city, string? state, string postalCode)
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

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 50 characters.")]
        [RegularExpression(@"^[a-zA-Z\u00c0-\u00d6\u00d8-\u00f6\u00f8-\u00ff\s\-']+$", ErrorMessage = "Name can only contain letters, spaces, hyphens and apostrophes.")]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^\+?\d[\d\s\-]{6,18}\d$", ErrorMessage = "Please enter a valid phone number (e.g. +47 12345678).")]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Street address is required.")]
        [StringLength(100)]
        [Display(Name = "Street Address")]
        public string StreetAddress { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(50)]
        public string City { get; set; }

        [StringLength(50)]
        [Display(Name = "State / Region")]
        public string? State { get; set; }

        [Required(ErrorMessage = "Postal code is required.")]
        [StringLength(10)]
        [RegularExpression(@"^\d{4,10}$", ErrorMessage = "Postal code must be 4-10 digits.")]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; }

        [Required]
        [StringLength(50)]
        public string Country { get; set; } = "Norway";
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