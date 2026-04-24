using Cartiva.Domain;

namespace Cartiva.Application.Abstractions
{
    public interface IInvoiceService
    {
        /// <summary>
        /// Generates an invoice from an order for company users with deferred payment
        /// </summary>
        Task<Invoice> GenerateInvoiceFromOrderAsync(int orderId, CancellationToken ct = default);

        /// <summary>
        /// Gets an invoice by ID with all related data
        /// </summary>
        Task<Invoice?> GetInvoiceByIdAsync(int invoiceId, CancellationToken ct = default);

        /// <summary>
        /// Gets an invoice by order ID
        /// </summary>
        Task<Invoice?> GetInvoiceByOrderIdAsync(int orderId, CancellationToken ct = default);

        /// <summary>
        /// Marks an invoice as sent and records the sent date
        /// </summary>
        Task<bool> MarkInvoiceAsSentAsync(int invoiceId, CancellationToken ct = default);

        /// <summary>
        /// Sends an invoice email and marks the invoice as sent.
        /// </summary>
        Task<bool> SendInvoiceAsync(int invoiceId, CancellationToken ct = default);

        /// <summary>
        /// Records a payment against an invoice
        /// </summary>
        Task<InvoicePayment> RecordPaymentAsync(
            int invoiceId,
            decimal amount,
            PaymentMethod paymentMethod,
            string? transactionId = null,
            string? paymentReference = null,
            string? registeredBy = null,
            CancellationToken ct = default);

        /// <summary>
        /// Cancels an invoice
        /// </summary>
        Task<bool> CancelInvoiceAsync(int invoiceId, string cancelledBy, string? reason = null, CancellationToken ct = default);

        /// <summary>
        /// Gets all overdue invoices that haven't been reminded
        /// </summary>
        Task<List<Invoice>> GetOverdueInvoicesAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets all invoices due within the specified number of days
        /// </summary>
        Task<List<Invoice>> GetInvoicesDueSoonAsync(int daysUntilDue, CancellationToken ct = default);

        /// <summary>
        /// Updates invoice status based on payments and due date
        /// </summary>
        Task RefreshInvoiceStatusAsync(int invoiceId, CancellationToken ct = default);

        /// <summary>
        /// Gets the next invoice sequence number for the current year
        /// </summary>
        Task<int> GetNextInvoiceSequenceAsync(CancellationToken ct = default);

        /// <summary>
        /// Gets the next credit note sequence number for the current year
        /// </summary>
        Task<int> GetNextCreditNoteSequenceAsync(CancellationToken ct = default);
    }
}
