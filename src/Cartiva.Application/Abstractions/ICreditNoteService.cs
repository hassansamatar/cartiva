using Cartiva.Domain;

namespace Cartiva.Application.Abstractions
{
    public interface ICreditNoteService
    {
        /// <summary>
        /// Creates a credit note from a return request
        /// </summary>
        Task<CreditNote> CreateFromReturnRequestAsync(int returnRequestId, CancellationToken ct = default);

        /// <summary>
        /// Creates a credit note manually (for partial credits, adjustments, etc.)
        /// </summary>
        Task<CreditNote> CreateCreditNoteAsync(
            int invoiceId,
            string reason,
            List<(int invoiceLineId, int quantity)> linesToCredit,
            string? createdByUserId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Creates a credit note for a fully cancelled order.
        /// </summary>
        Task<CreditNote> CreateFromCancelledOrderAsync(
            int orderId,
            string reason,
            string? createdByUserId = null,
            CancellationToken ct = default);

        /// <summary>
        /// Gets a credit note by ID with all related data
        /// </summary>
        Task<CreditNote?> GetCreditNoteByIdAsync(int creditNoteId, CancellationToken ct = default);

        /// <summary>
        /// Gets a credit note by the originating return request ID with all related data
        /// </summary>
        Task<CreditNote?> GetCreditNoteByReturnRequestIdAsync(int returnRequestId, CancellationToken ct = default);

        /// <summary>
        /// Gets all credit notes for an invoice
        /// </summary>
        Task<List<CreditNote>> GetCreditNotesForInvoiceAsync(int invoiceId, CancellationToken ct = default);

        /// <summary>
        /// Gets all credit notes with related invoice and return context.
        /// </summary>
        Task<List<CreditNote>> GetAllCreditNotesAsync(CancellationToken ct = default);

        /// <summary>
        /// Issues a draft credit note (makes it official)
        /// </summary>
        Task<bool> IssueCreditNoteAsync(int creditNoteId, CancellationToken ct = default);

        /// <summary>
        /// Cancels a credit note (only if not booked)
        /// </summary>
        Task<bool> CancelCreditNoteAsync(int creditNoteId, CancellationToken ct = default);

        /// <summary>
        /// Marks a credit note as booked in accounting system
        /// </summary>
        Task<bool> MarkAsBookedAsync(int creditNoteId, string? externalAccountingId = null, CancellationToken ct = default);
    }
}
