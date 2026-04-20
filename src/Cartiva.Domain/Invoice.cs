using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cartiva.Domain
{
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        // Optional relation to order
        public int? OrderHeaderId { get; set; }

        [ForeignKey(nameof(OrderHeaderId))]
        [ValidateNever]
        public OrderHeader? OrderHeader { get; set; }

        // =========================
        // IDENTIFIERS
        // =========================
        [Required, MaxLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required, MaxLength(16)]
        public string KID { get; set; } = string.Empty;

        // =========================
        // DATES
        // =========================
        public DateOnly IssueDate { get; set; }
        public DateOnly DueDate { get; set; }

        // =========================
        // AMOUNTS
        // =========================
        [Column(TypeName = "decimal(18,2)")]
        public decimal NetAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal VatAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [MaxLength(3)]
        public string Currency { get; set; } = "NOK";

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

        // =========================
        // SELLER SNAPSHOT
        // =========================
        [Required]
        public string SellerName { get; set; } = string.Empty;

        [Required]
        public string SellerOrgNumber { get; set; } = string.Empty;

        public string? SellerAddress { get; set; }
        public string? SellerEmail { get; set; }
        public string? SellerPhone { get; set; }

        // =========================
        // CUSTOMER SNAPSHOT
        // =========================
        [Required]
        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerOrgNumber { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerEmail { get; set; }

        // =========================
        // BANK SNAPSHOT
        // =========================
        [MaxLength(20)]
        public string? BankAccountNumber { get; set; }

        [MaxLength(34)]
        public string? IBAN { get; set; }

        [MaxLength(11)]
        public string? BIC { get; set; }

        // =========================
        // DELIVERY
        // =========================
        public DateTime? SentDate { get; set; }
        public string? PdfUrl { get; set; }

        // =========================
        // PAYMENT
        // =========================
        public DateTime? PaidDate { get; set; }

        // =========================
        // CANCELLATION
        // =========================
        public DateTime? CancelledAt { get; set; }
        public string? CancelledBy { get; set; }
        public string? CancellationReason { get; set; }

        // =========================
        // ACCOUNTING
        // =========================
        public bool IsBooked { get; set; }
        public DateTime? BookedAt { get; set; }
        public string? ExternalAccountingId { get; set; }

        // =========================
        // EHF / PEPPOL
        // =========================
        public string? PeppolId { get; set; }
        public bool IsEhfSent { get; set; }
        public DateTime? EhfSentAt { get; set; }

        // =========================
        // AUDIT
        // =========================
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // =========================
        // NAVIGATION
        // =========================
        [ValidateNever]
        public ICollection<InvoiceLine> Lines { get; set; } = new List<InvoiceLine>();

        [ValidateNever]
        public ICollection<InvoicePayment> Payments { get; set; } = new List<InvoicePayment>();

        [ValidateNever]
        public ICollection<CreditNote> CreditNotes { get; set; } = new List<CreditNote>();

        // =========================
        // COMPUTED
        // =========================
        [NotMapped]
        public decimal TotalPaid => Payments?.Sum(p => p.Amount) ?? 0;

        [NotMapped]
        public decimal TotalCredited => CreditNotes?
            .Where(c => c.Status != CreditNoteStatus.Cancelled)
            .Sum(c => c.TotalAmount) ?? 0;

        [NotMapped]
        public decimal RemainingAmount => TotalAmount - TotalPaid - TotalCredited;

        [NotMapped]
        public bool IsFullyPaid => RemainingAmount <= 0;

        [NotMapped]
        public bool IsOverdue => Status != InvoiceStatus.Paid &&
                                 Status != InvoiceStatus.Cancelled &&
                                 DueDate < DateOnly.FromDateTime(DateTime.UtcNow);

        // =========================
        // HELPER METHODS
        // =========================
        public void RecalculateStatus()
        {
            if (Status == InvoiceStatus.Cancelled)
                return;

            if (IsFullyPaid)
            {
                Status = InvoiceStatus.Paid;
                PaidDate ??= DateTime.UtcNow;
            }
            else if (TotalPaid > 0 || TotalCredited > 0)
            {
                Status = InvoiceStatus.PartiallyPaid;
            }
            else if (IsOverdue)
            {
                Status = InvoiceStatus.Overdue;
            }
        }

        public void Cancel(string cancelledBy, string? reason = null)
        {
            Status = InvoiceStatus.Cancelled;
            CancelledAt = DateTime.UtcNow;
            CancelledBy = cancelledBy;
            CancellationReason = reason;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
