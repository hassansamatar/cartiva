using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Cartiva.Domain.Enums;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cartiva.Domain
{
    public class CreditNote
    {
        [Key]
        public int Id { get; set; }

        // =========================
        // LINKS
        // =========================
        public int OriginalInvoiceId { get; set; }

        [ForeignKey(nameof(OriginalInvoiceId))]
        [ValidateNever]
        public Invoice OriginalInvoice { get; set; } = null!;

        /// <summary>
        /// Link back to return request (if credit note was generated from a return)
        /// </summary>
        public int? ReturnRequestId { get; set; }

        [ForeignKey(nameof(ReturnRequestId))]
        [ValidateNever]
        public ReturnRequest? ReturnRequest { get; set; }

        /// <summary>
        /// External payment reference (Stripe refund ID, etc.)
        /// </summary>
        [MaxLength(100)]
        public string? ExternalRefundReference { get; set; }

        // =========================
        // DOCUMENT INFO
        // =========================
        [Required, MaxLength(50)]
        public string CreditNoteNumber { get; set; } = string.Empty;

        public DateOnly IssueDate { get; set; }

        public CreditNoteStatus Status { get; set; } = CreditNoteStatus.Draft;

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

        // =========================
        // REASON + AUDIT
        // =========================
        [MaxLength(255)]
        public string Reason { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [MaxLength(100)]
        public string? CreatedByUserId { get; set; }

        // =========================
        // ACCOUNTING
        // =========================
        public bool IsBooked { get; set; }
        public DateTime? BookedAt { get; set; }
        public string? ExternalAccountingId { get; set; }

        // =========================
        // CUSTOMER SNAPSHOT (copied from invoice)
        // =========================
        [Required]
        public string CustomerName { get; set; } = string.Empty;

        public string? CustomerOrgNumber { get; set; }
        public string? CustomerAddress { get; set; }

        // =========================
        // NAVIGATION
        // =========================
        [ValidateNever]
        public ICollection<CreditNoteLine> Lines { get; set; } = new List<CreditNoteLine>();

        // =========================
        // COMPUTED
        // =========================
        [NotMapped]
        public bool CanBeEdited => Status == CreditNoteStatus.Draft;

        // =========================
        // HELPER METHODS
        // =========================
        public void RecalculateTotals()
        {
            NetAmount = Lines.Sum(l => l.LineNetAmount);
            VatAmount = Lines.Sum(l => l.LineVatAmount);
            TotalAmount = Lines.Sum(l => l.LineTotalAmount);
            UpdatedAt = DateTime.UtcNow;
        }

        public void Issue()
        {
            if (Status != CreditNoteStatus.Draft)
                throw new InvalidOperationException("Only draft credit notes can be issued.");

            Status = CreditNoteStatus.Issued;
            IssueDate = DateOnly.FromDateTime(DateTime.UtcNow);
            UpdatedAt = DateTime.UtcNow;
        }

        public void Cancel()
        {
            if (Status == CreditNoteStatus.Booked)
                throw new InvalidOperationException("Booked credit notes cannot be cancelled.");

            Status = CreditNoteStatus.Cancelled;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Creates a credit note from a return request
        /// </summary>
        public static CreditNote FromReturnRequest(ReturnRequest returnRequest, Invoice originalInvoice)
        {
            var creditNote = new CreditNote
            {
                OriginalInvoiceId = originalInvoice.Id,
                ReturnRequestId = returnRequest.Id,
                IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Reason = returnRequest.Reason,
                Notes = returnRequest.Description,
                CustomerName = originalInvoice.CustomerName,
                CustomerOrgNumber = originalInvoice.CustomerOrgNumber,
                CustomerAddress = originalInvoice.CustomerAddress,
                Currency = originalInvoice.Currency
            };

            return creditNote;
        }
    }
}
