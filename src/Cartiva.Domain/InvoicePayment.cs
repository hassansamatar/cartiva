using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cartiva.Domain
{
    public class InvoicePayment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        [ValidateNever]
        public Invoice Invoice { get; set; } = null!;

        // =========================
        // PAYMENT DETAILS
        // =========================
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime PaymentDate { get; set; }

        /// <summary>
        /// Usually contains KID, but may also contain other bank references
        /// </summary>
        [MaxLength(50)]
        public string? PaymentReference { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Unknown;

        /// <summary>
        /// External transaction ID (Stripe, Vipps, bank reference, etc.)
        /// </summary>
        [MaxLength(100)]
        public string? TransactionId { get; set; }

        // =========================
        // IDEMPOTENCY
        // =========================
        /// <summary>
        /// Unique key to prevent duplicate payment registration
        /// Format suggestion: {InvoiceId}-{TransactionId} or {InvoiceId}-{Date}-{Amount}
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string IdempotencyKey { get; set; } = string.Empty;

        // =========================
        // AUDIT
        // =========================
        [MaxLength(100)]
        public string? RegisteredBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Notes { get; set; }

        // =========================
        // HELPER METHODS
        // =========================
        public static string GenerateIdempotencyKey(int invoiceId, string? transactionId)
        {
            if (!string.IsNullOrEmpty(transactionId))
                return $"{invoiceId}-{transactionId}";

            return $"{invoiceId}-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        }
    }
}
