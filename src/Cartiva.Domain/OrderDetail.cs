using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Cartiva.Domain
{
    public class OrderDetail
    {
        public int Id { get; set; }

        [Required]
        public int OrderHeaderId { get; set; }

        [ForeignKey("OrderHeaderId")]
        [ValidateNever]
        public OrderHeader? OrderHeader { get; set; }

        [Required]
        public int ProductVariantId { get; set; }

        [ForeignKey("ProductVariantId")]
        [ValidateNever]
        public ProductVariant ProductVariant { get; set; }

        [Required]
        public int Count { get; set; }

        // =========================
        // PRICING WITH VAT BREAKDOWN (Snapshot at order time)
        // =========================

        /// <summary>
        /// Unit price excluding VAT (snapshot from ProductVariant)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceExVat { get; set; }

        /// <summary>
        /// VAT rate as percentage at time of order (e.g., 25.00)
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal VatRate { get; set; } = 25.00m;

        /// <summary>
        /// Unit price including VAT (snapshot from ProductVariant)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal PriceIncVat { get; set; }

        /// <summary>
        /// Discount percentage applied at order time
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal DiscountPercent { get; set; } = 0;

        /// <summary>
        /// Unit discount amount (calculated from PriceIncVat * DiscountPercent)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitDiscountAmount { get; set; } = 0;

        /// <summary>
        /// Legacy Price field - unit price after discount (for backward compatibility)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        // =========================
        // COMPUTED LINE TOTALS
        // =========================

        /// <summary>
        /// Line total excluding VAT: (PriceExVat - UnitDiscountExVat) * Count
        /// </summary>
        [NotMapped]
        public decimal LineTotalExVat => (PriceExVat * Count) - TotalDiscountExVat;

        /// <summary>
        /// Unit discount excluding VAT
        /// </summary>
        [NotMapped]
        public decimal UnitDiscountExVat => UnitDiscountAmount / (1 + VatRate / 100m);

        /// <summary>
        /// Total discount for this line excluding VAT
        /// </summary>
        [NotMapped]
        public decimal TotalDiscountExVat => UnitDiscountExVat * Count;

        /// <summary>
        /// Total discount for this line including VAT
        /// </summary>
        [NotMapped]
        public decimal TotalDiscountIncVat => UnitDiscountAmount * Count;

        /// <summary>
        /// VAT amount for this line
        /// </summary>
        [NotMapped]
        public decimal LineVatAmount => LineTotalExVat * (VatRate / 100m);

        /// <summary>
        /// Line total including VAT: LineTotalExVat + LineVatAmount
        /// </summary>
        [NotMapped]
        public decimal LineTotalIncVat => LineTotalExVat + LineVatAmount;

        /// <summary>
        /// Original line total before discount (including VAT)
        /// </summary>
        [NotMapped]
        public decimal OriginalLineTotalIncVat => PriceIncVat * Count;

        /// <summary>
        /// Whether this line has a discount applied
        /// </summary>
        [NotMapped]
        public bool HasDiscount => DiscountPercent > 0 || UnitDiscountAmount > 0;

        // =========================
        // PRODUCT SNAPSHOT (for invoice/display)
        // =========================

        /// <summary>
        /// Product name at time of order (snapshot)
        /// </summary>
        [StringLength(200)]
        public string? ProductName { get; set; }

        /// <summary>
        /// Product variant description (color/size) at time of order
        /// </summary>
        [StringLength(100)]
        public string? VariantDescription { get; set; }

        // =========================
        // HELPER METHODS
        // =========================

        /// <summary>
        /// Creates an OrderDetail from a ProductVariant with VAT breakdown
        /// </summary>
        public static OrderDetail FromProductVariant(ProductVariant variant, int count)
        {
            return new OrderDetail
            {
                ProductVariantId = variant.Id,
                Count = count,
                PriceExVat = variant.PriceExVat,
                VatRate = variant.VatRate,
                PriceIncVat = variant.PriceIncVat,
                DiscountPercent = variant.DiscountPercent,
                UnitDiscountAmount = variant.DiscountAmount,
                Price = variant.FinalPrice, // Legacy field - final price after discount
                ProductName = variant.Product?.Name,
                VariantDescription = $"{variant.Color}" + (variant.SizeValue != null ? $" - {variant.SizeValue.Value}" : "")
            };
        }

        /// <summary>
        /// Creates an OrderDetail with a custom discount
        /// </summary>
        public static OrderDetail FromProductVariantWithDiscount(ProductVariant variant, int count, decimal discountPercent)
        {
            var detail = FromProductVariant(variant, count);
            detail.DiscountPercent = discountPercent;
            detail.UnitDiscountAmount = variant.PriceIncVat * (discountPercent / 100m);
            detail.Price = variant.PriceIncVat - detail.UnitDiscountAmount;
            return detail;
        }
    }
}
