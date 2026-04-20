using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cartiva.Domain
{
    public class InvoiceLine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        [ValidateNever]
        public Invoice Invoice { get; set; } = null!;

        // =========================
        // PRODUCT SNAPSHOT (immutable at invoice time)
        // =========================
        public int? ProductVariantId { get; set; }

        [ForeignKey(nameof(ProductVariantId))]
        [ValidateNever]
        public ProductVariant? ProductVariant { get; set; }

        [Required, MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? ProductSku { get; set; }

        [MaxLength(100)]
        public string? ProductDescription { get; set; }

        // =========================
        // QUANTITY & PRICING
        // =========================
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Discount amount per unit (if any)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; } = 0;

        // =========================
        // VAT / MVA
        // =========================
        /// <summary>
        /// VAT percentage (e.g., 25.00 for 25% Norwegian standard rate)
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal VatPercent { get; set; } = 25.00m;

        // =========================
        // CALCULATED AMOUNTS
        // =========================
        /// <summary>
        /// Net amount before VAT: (UnitPrice - DiscountAmount) * Quantity
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal LineNetAmount { get; set; }

        /// <summary>
        /// VAT amount: LineNetAmount * (VatPercent / 100)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal LineVatAmount { get; set; }

        /// <summary>
        /// Total amount including VAT: LineNetAmount + LineVatAmount
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotalAmount { get; set; }

        // =========================
        // SORTING
        // =========================
        public int SortOrder { get; set; }

        // =========================
        // HELPER METHODS
        // =========================
        public void Calculate()
        {
            LineNetAmount = (UnitPrice - DiscountAmount) * Quantity;
            LineVatAmount = LineNetAmount * (VatPercent / 100m);
            LineTotalAmount = LineNetAmount + LineVatAmount;
        }

        /// <summary>
        /// Creates an invoice line from an order detail with full VAT breakdown
        /// </summary>
        public static InvoiceLine FromOrderDetail(OrderDetail orderDetail, decimal? vatPercentOverride = null)
        {
            // Use OrderDetail's VAT data, or fallback to override/default
            var vatPercent = vatPercentOverride ?? orderDetail.VatRate;
            if (vatPercent == 0) vatPercent = 25.00m; // Default to Norwegian standard rate

            // Use the product snapshot from OrderDetail if available
            var productName = !string.IsNullOrEmpty(orderDetail.ProductName) 
                ? orderDetail.ProductName 
                : orderDetail.ProductVariant?.Product?.Name ?? "Unknown Product";

            var productDescription = !string.IsNullOrEmpty(orderDetail.VariantDescription)
                ? orderDetail.VariantDescription
                : $"{orderDetail.ProductVariant?.Color}" + 
                  (orderDetail.ProductVariant?.SizeValue != null ? $" - {orderDetail.ProductVariant.SizeValue.Value}" : "");

            var line = new InvoiceLine
            {
                ProductVariantId = orderDetail.ProductVariantId,
                ProductName = productName,
                ProductSku = orderDetail.ProductVariant?.Product?.Id.ToString(),
                ProductDescription = productDescription,
                Quantity = orderDetail.Count,
                // Use PriceExVat from OrderDetail (unit price excluding VAT)
                UnitPrice = orderDetail.PriceExVat > 0 ? orderDetail.PriceExVat : orderDetail.Price / (1 + vatPercent / 100m),
                // Use discount from OrderDetail
                DiscountAmount = orderDetail.UnitDiscountExVat,
                VatPercent = vatPercent
            };
            line.Calculate();
            return line;
        }

        /// <summary>
        /// Creates an invoice line manually with explicit values
        /// </summary>
        public static InvoiceLine Create(
            string productName,
            int quantity,
            decimal unitPriceExVat,
            decimal vatPercent = 25.00m,
            decimal discountAmount = 0,
            string? description = null,
            string? sku = null,
            int? productVariantId = null)
        {
            var line = new InvoiceLine
            {
                ProductVariantId = productVariantId,
                ProductName = productName,
                ProductSku = sku,
                ProductDescription = description,
                Quantity = quantity,
                UnitPrice = unitPriceExVat,
                DiscountAmount = discountAmount,
                VatPercent = vatPercent
            };
            line.Calculate();
            return line;
        }
    }
}
