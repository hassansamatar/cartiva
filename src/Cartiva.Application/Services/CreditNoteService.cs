using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Domain.Enums;
using Cartiva.Domain.Extensions;
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
                        .ThenInclude(oh => oh.ApplicationUser)
                            .ThenInclude(u => u.Company)
                .Include(r => r.OrderDetail)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv!.Product)
                .FirstOrDefaultAsync(r => r.Id == returnRequestId, ct);

            if (returnRequest == null)
                throw new InvalidOperationException($"Return request with ID {returnRequestId} not found.");

            if (returnRequest.Status != ReturnStatus.Approved)
                throw new InvalidOperationException("Credit notes can only be created for approved return requests.");

            // =========================
            // GUARD: Prevent credit note creation for B2B company returns WITH DEFERRED PAYMENT
            // Company orders paid UPFRONT are allowed credit notes (treated like individuals)
            // =========================
            var user = returnRequest.OrderDetail.OrderHeader.ApplicationUser;
            var order = returnRequest.OrderDetail.OrderHeader;

            bool isCompanyDeferredPayment = 
                user?.CompanyId.HasValue == true && 
                user.Company?.IsActive == true &&
                (order.PaymentStatus == PaymentStatus.Deferred || order.PaymentStatus == PaymentStatus.Pending);

            if (isCompanyDeferredPayment)
            {
                throw new InvalidOperationException(
                    $"Cannot create credit note for company return with deferred payment. " +
                    $"Company returns with deferred payment use Accounts Receivable Adjustments instead. " +
                    $"Return request {returnRequestId} belongs to company {user.Company.Name}. " +
                    $"Payment Status: {order.PaymentStatus}");
            }

            var orderId = returnRequest.OrderDetail.OrderHeaderId;

            // Check if credit note already exists for this return (idempotency)
            var existingCreditNote = await _db.Set<CreditNote>()
                .FirstOrDefaultAsync(c => c.ReturnRequestId == returnRequestId, ct);

            if (existingCreditNote != null)
            {
                _logger.LogWarning("Credit note already exists for Return Request {ReturnRequestId}", returnRequestId);
                return existingCreditNote;
            }

            // Get the invoice for this order — AUTO-GENERATE if missing.
            // This unifies the flow for both Customer and Company users.
            var invoice = await _db.Set<Invoice>()
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.OrderHeaderId == orderId, ct);

            if (invoice == null)
            {
                _logger.LogInformation(
                    "No invoice found for Order {OrderId}. Auto-generating invoice before creating credit note.",
                    orderId);

                // GenerateInvoiceFromOrderAsync works for both Customer and Company users
                invoice = await _invoiceService.GenerateInvoiceFromOrderAsync(orderId, ct);

                // Reload with Lines to ensure navigation is populated
                invoice = await _db.Set<Invoice>()
                    .Include(i => i.Lines)
                    .FirstAsync(i => i.Id == invoice.Id, ct);
            }

            var sequence = await _invoiceService.GetNextCreditNoteSequenceAsync(ct);
            var creditNoteNumber = SD.GenerateCreditNoteNumber(sequence);

            // Unified logic: always create credit note from the invoice
            var creditNote = CreditNote.FromReturnRequest(returnRequest, invoice);
            creditNote.CreditNoteNumber = creditNoteNumber;

            // Find the matching invoice line
            var invoiceLine = invoice.Lines.FirstOrDefault(l =>
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

        public async Task<CreditNote> CreateFromCancelledOrderAsync(
            int orderId,
            string reason,
            string? createdByUserId = null,
            CancellationToken ct = default)
        {
            var order = await _db.OrderHeaders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .Include(o => o.ApplicationUser)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);

            if (order == null)
                throw new InvalidOperationException($"Order with ID {orderId} not found.");

            var invoice = await _db.Set<Invoice>()
                .Include(i => i.Lines)
                .FirstOrDefaultAsync(i => i.OrderHeaderId == orderId, ct);

            if (invoice == null)
            {
                invoice = await _invoiceService.GenerateInvoiceFromOrderAsync(orderId, ct);
                invoice = await _db.Set<Invoice>()
                    .Include(i => i.Lines)
                    .FirstAsync(i => i.Id == invoice.Id, ct);
            }

            var existingCreditNote = await _db.Set<CreditNote>()
                .FirstOrDefaultAsync(c => c.OriginalInvoiceId == invoice.Id && c.Reason == reason, ct);

            if (existingCreditNote != null)
            {
                _logger.LogWarning("Credit note already exists for cancelled order {OrderId}", orderId);
                return existingCreditNote;
            }

            var sequence = await _invoiceService.GetNextCreditNoteSequenceAsync(ct);
            var creditNoteNumber = SD.GenerateCreditNoteNumber(sequence);

            var creditNote = new CreditNote
            {
                OriginalInvoiceId = invoice.Id,
                CreditNoteNumber = creditNoteNumber,
                IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Reason = reason,
                Notes = $"Cancelled order #{orderId}",
                CreatedByUserId = createdByUserId,
                CustomerName = invoice.CustomerName,
                CustomerOrgNumber = invoice.CustomerOrgNumber,
                CustomerAddress = invoice.CustomerAddress,
                Currency = invoice.Currency
            };

            int sortOrder = 1;
            foreach (var orderDetail in order.OrderDetails)
            {
                var invoiceLine = invoice.Lines.FirstOrDefault(l => l.ProductVariantId == orderDetail.ProductVariantId);

                CreditNoteLine creditLine;
                if (invoiceLine != null)
                {
                    creditLine = CreditNoteLine.FromInvoiceLine(invoiceLine, orderDetail.Count);
                }
                else
                {
                    creditLine = new CreditNoteLine
                    {
                        Description = orderDetail.ProductName ?? orderDetail.ProductVariant?.Product?.Name ?? "Cancelled Item",
                        Quantity = orderDetail.Count,
                        UnitPrice = orderDetail.PriceExVat > 0 ? orderDetail.PriceExVat : orderDetail.Price,
                        VatPercent = orderDetail.VatRate > 0 ? orderDetail.VatRate : SD.VatRateStandard
                    };
                    creditLine.Calculate();
                }

                creditLine.SortOrder = sortOrder++;
                creditNote.Lines.Add(creditLine);
            }

            creditNote.RecalculateTotals();
            creditNote.Issue();

            _db.Set<CreditNote>().Add(creditNote);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Created credit note {CreditNoteNumber} for cancelled order {OrderId}", creditNoteNumber, orderId);

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
                .Include(c => c.OriginalInvoice)
                    .ThenInclude(i => i.OrderHeader)
                .Include(c => c.ReturnRequest)
                    .ThenInclude(r => r.OrderDetail)
                        .ThenInclude(od => od.OrderHeader)
                .Where(c => c.OriginalInvoiceId == invoiceId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(ct);
        }

        public async Task<List<CreditNote>> GetAllCreditNotesAsync(CancellationToken ct = default)
        {
            return await _db.Set<CreditNote>()
                .Include(c => c.Lines)
                .Include(c => c.OriginalInvoice)
                    .ThenInclude(i => i.OrderHeader)
                .Include(c => c.ReturnRequest)
                    .ThenInclude(r => r.OrderDetail)
                        .ThenInclude(od => od.OrderHeader)
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
