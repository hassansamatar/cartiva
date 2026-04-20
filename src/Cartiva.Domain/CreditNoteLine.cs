using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cartiva.Domain
{
    public class CreditNoteLine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CreditNoteId { get; set; }

        [ForeignKey(nameof(CreditNoteId))]
        [ValidateNever]
        public CreditNote CreditNote { get; set; } = null!;

        // =========================
        // LINK TO ORIGINAL INVOICE LINE
        // =========================
        public int? OriginalInvoiceLineId { get; set; }

        [ForeignKey(nameof(OriginalInvoiceLineId))]
        [ValidateNever]
        public InvoiceLine? OriginalInvoiceLine { get; set; }

        // =========================
        // PRODUCT INFO
        // =========================
        [Required, MaxLength(200)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? ProductSku { get; set; }

        // =========================
        // QUANTITY & PRICING
        // =========================
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        // =========================
        // VAT / MVA
        // =========================
        [Column(TypeName = "decimal(5,2)")]
        public decimal VatPercent { get; set; } = 25.00m;

        // =========================
        // CALCULATED AMOUNTS
        // =========================
        [Column(TypeName = "decimal(18,2)")]
        public decimal LineNetAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineVatAmount { get; set; }

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
            LineNetAmount = UnitPrice * Quantity;
            LineVatAmount = LineNetAmount * (VatPercent / 100m);
            LineTotalAmount = LineNetAmount + LineVatAmount;
        }

        /// <summary>
        /// Creates a credit note line from an invoice line (for full or partial credit)
        /// </summary>
        public static CreditNoteLine FromInvoiceLine(InvoiceLine invoiceLine, int? quantityToCredit = null)
        {
            var qty = quantityToCredit ?? invoiceLine.Quantity;

            var line = new CreditNoteLine
            {
                OriginalInvoiceLineId = invoiceLine.Id,
                Description = invoiceLine.ProductName,
                ProductSku = invoiceLine.ProductSku,
                Quantity = qty,
                UnitPrice = invoiceLine.UnitPrice - invoiceLine.DiscountAmount,
                VatPercent = invoiceLine.VatPercent,
                SortOrder = invoiceLine.SortOrder
            };
            line.Calculate();
            return line;
        }
    }
}
