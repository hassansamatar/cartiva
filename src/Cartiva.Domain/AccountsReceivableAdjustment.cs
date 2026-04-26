using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Cartiva.Domain.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cartiva.Domain
{
    /// <summary>
    /// Represents a financial correction to accounts receivable after B2B company return approval.
    /// This is NOT a refund - it reduces the company's outstanding balance.
    /// </summary>
    public class AccountsReceivableAdjustment
    {
        [Key]
        public int Id { get; set; }

        // =========================
        // LINKS
        // =========================

        /// <summary>
        /// The company whose receivable balance is being adjusted
        /// </summary>
        [Required]
        public int CompanyId { get; set; }

        [ForeignKey(nameof(CompanyId))]
        [ValidateNever]
        public Company Company { get; set; } = null!;

        /// <summary>
        /// The original invoice being adjusted (immutable - never modified)
        /// </summary>
        [Required]
        public int InvoiceId { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        [ValidateNever]
        public Invoice Invoice { get; set; } = null!;

        /// <summary>
        /// The return request that triggered this adjustment
        /// </summary>
        [Required]
        public int ReturnRequestId { get; set; }

        [ForeignKey(nameof(ReturnRequestId))]
        [ValidateNever]
        public ReturnRequest ReturnRequest { get; set; } = null!;

        // =========================
        // AMOUNTS
        // =========================

        /// <summary>
        /// Adjustment amount (negative value reducing receivables)
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Currency code (default NOK)
        /// </summary>
        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "NOK";

        // =========================
        // REASON & STATUS
        // =========================

        /// <summary>
        /// Reason for the adjustment (e.g., "Return approved for Order #123")
        /// </summary>
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        /// <summary>
        /// Current status of the adjustment
        /// </summary>
        [Required]
        [StringLength(30)]
        public ARAdjustmentStatus Status { get; set; } = ARAdjustmentStatus.Pending;

        // =========================
        // TIMESTAMPS
        // =========================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the adjustment was applied to Stripe credit balance
        /// </summary>
        public DateTime? AppliedAt { get; set; }

        // =========================
        // STRIPE INTEGRATION
        // =========================

        /// <summary>
        /// Whether this adjustment has been applied as Stripe credit balance
        /// </summary>
        public bool StripeCreditBalanceApplied { get; set; } = false;

        /// <summary>
        /// Stripe customer balance transaction ID
        /// </summary>
        [StringLength(100)]
        public string? StripeCustomerBalanceReference { get; set; }

        // =========================
        // AUDIT
        // =========================

        /// <summary>
        /// Admin user who created the adjustment
        /// </summary>
        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        /// <summary>
        /// Additional notes or comments
        /// </summary>
        [StringLength(1000)]
        public string? Notes { get; set; }
    }
}
