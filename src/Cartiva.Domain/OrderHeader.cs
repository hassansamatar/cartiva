using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Cartiva.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cartiva.Shared;

namespace Cartiva.Domain
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

        // =========================
        // ORDER TOTALS WITH VAT BREAKDOWN
        // =========================

        /// <summary>
        /// Subtotal excluding VAT (sum of all line totals ex VAT)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubtotalExVat { get; set; }

        /// <summary>
        /// Total VAT amount for the order
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalVatAmount { get; set; }

        /// <summary>
        /// Total discount amount (including VAT)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalDiscountAmount { get; set; }

        /// <summary>
        /// Shipping cost excluding VAT
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingCostExVat { get; set; } = 0;

        /// <summary>
        /// Shipping VAT amount
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingVatAmount { get; set; } = 0;

        /// <summary>
        /// Legacy OrderTotal - final amount customer pays (including VAT, after discounts)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal OrderTotal { get; set; }

        /// <summary>
        /// Currency code (default NOK for Norway)
        /// </summary>
        [StringLength(3)]
        public string Currency { get; set; } = "NOK";

        // =========================
        // COMPUTED TOTALS
        // =========================

        /// <summary>
        /// Subtotal including VAT before discounts
        /// </summary>
        [NotMapped]
        public decimal SubtotalIncVat => SubtotalExVat + TotalVatAmount + TotalDiscountAmount;

        /// <summary>
        /// Total shipping cost including VAT
        /// </summary>
        [NotMapped]
        public decimal ShippingCostIncVat => ShippingCostExVat + ShippingVatAmount;

        /// <summary>
        /// Grand total excluding VAT
        /// </summary>
        [NotMapped]
        public decimal GrandTotalExVat => SubtotalExVat + ShippingCostExVat;

        /// <summary>
        /// Grand total VAT
        /// </summary>
        [NotMapped]
        public decimal GrandTotalVat => TotalVatAmount + ShippingVatAmount;

        /// <summary>
        /// Whether order has any discounts applied
        /// </summary>
        [NotMapped]
        public bool HasDiscount => TotalDiscountAmount > 0;

        // =========================
        // ORDER STATUS & PAYMENT
        // =========================

        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }

        public DateTime? PaymentDate { get; set; }
        public DateOnly? PaymentDueDate { get; set; }
        public DateTime? ReturnExpirationDate { get; set; }
        public string? PaymentIntentId { get; set; }

        // =========================
        // CUSTOMER INFO
        // =========================

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

        // =========================
        // NAVIGATION & COLLECTIONS
        // =========================

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

        // Idempotence: Has invoice been sent for overdue payment?
        public bool InvoiceSent { get; set; } = false;

        // =========================
        // HELPER PROPERTIES
        // =========================

        public bool IsPending => OrderStatus == SD.StatusPending;
        public bool IsApproved => OrderStatus == SD.StatusApproved;
        public bool IsShipped => OrderStatus == SD.StatusShipped;
        public bool IsDelivered => OrderStatus == SD.StatusDelivered;
        public bool IsCancelled => OrderStatus == SD.StatusCancelled;

        public bool IsReturnWindowExpired => ReturnExpirationDate.HasValue && DateTime.Now > ReturnExpirationDate.Value;

        // =========================
        // HELPER METHODS
        // =========================

        public void MarkAsCancelled()
        {
            OrderStatus = SD.StatusCancelled;
            if (PaymentStatus == SD.PaymentStatusApproved)
                PaymentStatus = SD.PaymentStatusRefunded;
        }

        /// <summary>
        /// Recalculates order totals from OrderDetails
        /// </summary>
        public void RecalculateTotals()
        {
            if (OrderDetails == null || !OrderDetails.Any())
            {
                SubtotalExVat = 0;
                TotalVatAmount = 0;
                TotalDiscountAmount = 0;
                OrderTotal = ShippingCostIncVat;
                return;
            }

            SubtotalExVat = OrderDetails.Sum(d => d.LineTotalExVat);
            TotalVatAmount = OrderDetails.Sum(d => d.LineVatAmount);
            TotalDiscountAmount = OrderDetails.Sum(d => d.TotalDiscountIncVat);
            OrderTotal = SubtotalExVat + TotalVatAmount + ShippingCostIncVat;
        }

        /// <summary>
        /// Sets totals from a simple total amount (backward compatibility)
        /// Assumes standard Norwegian VAT rate of 25%
        /// </summary>
        public void SetTotalFromIncVat(decimal totalIncVat, decimal vatRate = 25.00m)
        {
            OrderTotal = totalIncVat;
            SubtotalExVat = totalIncVat / (1 + vatRate / 100m);
            TotalVatAmount = totalIncVat - SubtotalExVat;
        }
    }
}