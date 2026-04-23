using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Persistence;
using Cartiva.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cartiva.Application.Services
{
    public class CreditNoteService : ICreditNoteService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CreditNoteService> _logger;
        private readonly IInvoiceService _invoiceService;

        public CreditNoteService(
            ApplicationDbContext db,
            ILogger<CreditNoteService> logger,
            IInvoiceService invoiceService)
        {
            _db = db;
            _logger = logger;
            _invoiceService = invoiceService;
        }

        public async Task<CreditNote> CreateFromReturnRequestAsync(int returnRequestId, CancellationToken ct = default)
        {
            var returnRequest = await _db.ReturnRequests
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.OrderHeader)
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv!.Product)
                .FirstOrDefaultAsync(r => r.Id == returnRequestId, ct);

            if (returnRequest == null)
                throw new InvalidOperationException($"Return request with ID {returnRequestId} not found.");

            if (returnRequest.Status != SD.ReturnStatusApproved)
                throw new InvalidOperationException("Credit notes can only be created for approved return requests.");

            var orderId = returnRequest.OrderDetail.OrderHeaderId;

            // Get the invoice for this order (it is now optional)
            var invoice = await _db.Set<Invoice>()
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.OrderHeaderId == orderId, ct);

            // Check if credit note already exists for this return
            var existingCreditNote = await _db.Set<CreditNote>()
                .FirstOrDefaultAsync(c => c.ReturnRequestId == returnRequestId, ct);

            if (existingCreditNote != null)
            {
                _logger.LogWarning("Credit note already exists for Return Request {ReturnRequestId}", returnRequestId);
                return existingCreditNote;
            }

            var sequence = await _invoiceService.GetNextCreditNoteSequenceAsync(ct);
            var creditNoteNumber = SD.GenerateCreditNoteNumber(sequence);

            CreditNote creditNote;

            if (invoice != null)
            {
                // Use existing logic for invoice-based credit notes
                creditNote = CreditNote.FromReturnRequest(returnRequest, invoice);
            }
            else
            {
                // Create a new credit note manually for orders without an invoice
                var orderHeader = returnRequest.OrderDetail.OrderHeader;
                creditNote = new CreditNote
                {
                    ReturnRequestId = returnRequestId,
                    Reason = returnRequest.Reason,
                    CreatedByUserId = returnRequest.ApplicationUserId,
                    CustomerName = $"{orderHeader.Name} {orderHeader.Name}",
                    CustomerAddress = $"{orderHeader.StreetAddress}, {orderHeader.PostalCode} {orderHeader.City}",
                    Currency = orderHeader.Currency ?? SD.DefaultCurrency,
                    // No OriginalInvoiceId since there is no invoice
                };
            }

            creditNote.CreditNoteNumber = creditNoteNumber;

            // Find the matching invoice line if an invoice exists
            var invoiceLine = invoice?.Lines.FirstOrDefault(l =>
                l.ProductVariantId == returnRequest.OrderDetail.ProductVariantId);

            if (invoiceLine != null)
            {
                var creditLine = CreditNoteLine.FromInvoiceLine(invoiceLine, returnRequest.Quantity);
                creditNote.Lines.Add(creditLine);
            }
            else
            {
                // This is the primary path for non-invoice orders, and a fallback for invoice orders
                var creditLine = new CreditNoteLine
                {
                    Description = returnRequest.OrderDetail.ProductVariant?.Product?.Name ?? "Returned Item",
                    Quantity = returnRequest.Quantity,
                    UnitPrice = returnRequest.OrderDetail.Price,
                    VatPercent = SD.VatRateStandard
                };
                creditLine.Calculate();
                creditNote.Lines.Add(creditLine);
            }

            creditNote.RecalculateTotals();
            creditNote.Issue();
            // Set refund amount on return request
            returnRequest.RefundAmount = creditNote.TotalAmount;
            
            _db.Set<CreditNote>().Add(creditNote);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Created credit note {CreditNoteNumber} for Return Request {ReturnRequestId}",
                creditNoteNumber, returnRequestId);

            return creditNote;
        }

        public async Task<CreditNote> CreateCreditNoteAsync(
            int invoiceId,
            string reason,
            List<(int invoiceLineId, int quantity)> linesToCredit,
            string? createdByUserId = null,
            CancellationToken ct = default)
        {
            var invoice = await _db.Set<Invoice>()
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

            if (invoice == null)
                throw new InvalidOperationException($"Invoice with ID {invoiceId} not found.");

            var sequence = await _invoiceService.GetNextCreditNoteSequenceAsync(ct);
            var creditNoteNumber = SD.GenerateCreditNoteNumber(sequence);

            var creditNote = new CreditNote
            {
                OriginalInvoiceId = invoiceId,
                CreditNoteNumber = creditNoteNumber,
                IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Reason = reason,
                CreatedByUserId = createdByUserId,
                CustomerName = invoice.CustomerName,
                CustomerOrgNumber = invoice.CustomerOrgNumber,
                CustomerAddress = invoice.CustomerAddress,
                Currency = invoice.Currency
            };

            int sortOrder = 1;
            foreach (var (invoiceLineId, quantity) in linesToCredit)
            {
                var invoiceLine = invoice.Lines.FirstOrDefault(l => l.Id == invoiceLineId);
                if (invoiceLine == null) continue;

                var creditLine = CreditNoteLine.FromInvoiceLine(invoiceLine, quantity);
                creditLine.SortOrder = sortOrder++;
                creditNote.Lines.Add(creditLine);
            }

            creditNote.RecalculateTotals();

            _db.Set<CreditNote>().Add(creditNote);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Created credit note {CreditNoteNumber} for Invoice {InvoiceId}",
                creditNoteNumber, invoiceId);

            return creditNote;
        }

        public async Task<CreditNote?> GetCreditNoteByIdAsync(int creditNoteId, CancellationToken ct = default)
        {
            return await _db.Set<CreditNote>()
                .Include(c => c.Lines)
                .Include(c => c.OriginalInvoice)
                .Include(c => c.ReturnRequest)
                .FirstOrDefaultAsync(c => c.Id == creditNoteId, ct);
        }

        public async Task<CreditNote?> GetCreditNoteByReturnRequestIdAsync(int returnRequestId, CancellationToken ct = default)
        {
            return await _db.CreditNotes
                .Include(c => c.Lines)
                .Include(c => c.OriginalInvoice)
                .Include(c => c.ReturnRequest)
                .FirstOrDefaultAsync(c => c.ReturnRequestId == returnRequestId, ct);
        }

        public async Task<List<CreditNote>> GetCreditNotesForInvoiceAsync(int invoiceId, CancellationToken ct = default)
        {
            return await _db.Set<CreditNote>()
                .Include(c => c.Lines)
                .Where(c => c.OriginalInvoiceId == invoiceId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<bool> IssueCreditNoteAsync(int creditNoteId, CancellationToken ct = default)
        {
            var creditNote = await _db.Set<CreditNote>()
                .Include(c => c.OriginalInvoice)
                    .ThenInclude(i => i.Payments)
                .Include(c => c.OriginalInvoice)
                    .ThenInclude(i => i.CreditNotes)
                .FirstOrDefaultAsync(c => c.Id == creditNoteId, ct);

            if (creditNote == null) return false;

            creditNote.Issue();

            // Recalculate the original invoice status
            creditNote.OriginalInvoice.RecalculateStatus();
            creditNote.OriginalInvoice.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Credit note {CreditNoteId} issued", creditNoteId);
            return true;
        }

        public async Task<bool> CancelCreditNoteAsync(int creditNoteId, CancellationToken ct = default)
        {
            var creditNote = await _db.Set<CreditNote>()
                .Include(c => c.OriginalInvoice)
                    .ThenInclude(i => i.Payments)
                .Include(c => c.OriginalInvoice)
                    .ThenInclude(i => i.CreditNotes)
                .FirstOrDefaultAsync(c => c.Id == creditNoteId, ct);

            if (creditNote == null) return false;

            creditNote.Cancel();

            // Recalculate the original invoice status
            creditNote.OriginalInvoice.RecalculateStatus();
            creditNote.OriginalInvoice.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Credit note {CreditNoteId} cancelled", creditNoteId);
            return true;
        }

        public async Task<bool> MarkAsBookedAsync(int creditNoteId, string? externalAccountingId = null, CancellationToken ct = default)
        {
            var creditNote = await _db.Set<CreditNote>().FindAsync(new object[] { creditNoteId }, ct);
            if (creditNote == null) return false;

            if (creditNote.Status != CreditNoteStatus.Issued)
                throw new InvalidOperationException("Only issued credit notes can be booked.");

            creditNote.Status = CreditNoteStatus.Booked;
            creditNote.IsBooked = true;
            creditNote.BookedAt = DateTime.UtcNow;
            creditNote.ExternalAccountingId = externalAccountingId;
            creditNote.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Credit note {CreditNoteId} marked as booked", creditNoteId);
            return true;
        }
    }
}
