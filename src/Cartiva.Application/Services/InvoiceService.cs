using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Domain.Enums;
using Cartiva.Domain.Interfaces;
using Cartiva.Persistence;
using Cartiva.Shared;
using Cartiva.Shared.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cartiva.Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<InvoiceService> _logger;
        private readonly CartivaContact _cartivaContact;
        private readonly IConfiguration _configuration;
        private readonly INotificationService _notificationService;

        public InvoiceService(
            ApplicationDbContext db,
            ILogger<InvoiceService> logger,
            CartivaContact cartivaContact,
            IConfiguration configuration,
            INotificationService notificationService)
        {
            _db = db;
            _logger = logger;
            _cartivaContact = cartivaContact;
            _configuration = configuration;
            _notificationService = notificationService;
        }

        public async Task<Invoice> GenerateInvoiceFromOrderAsync(int orderId, CancellationToken ct = default)
        {
            var order = await _db.OrderHeaders
                .Include(o => o.ApplicationUser)
                    .ThenInclude(u => u!.Company)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv!.Product)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv!.SizeValue)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);

            if (order == null)
                throw new InvalidOperationException($"Order with ID {orderId} not found.");

            // Check if invoice already exists
            var existingInvoice = await _db.Set<Invoice>()
                .FirstOrDefaultAsync(i => i.OrderHeaderId == orderId, ct);

            if (existingInvoice != null)
            {
                _logger.LogWarning("Invoice already exists for Order {OrderId}. Returning existing invoice.", orderId);
                return existingInvoice;
            }

            var sequence = await GetNextInvoiceSequenceAsync(ct);
            var invoiceNumber = SD.GenerateInvoiceNumber(sequence);
            var kidNumber = SD.GenerateKIDNumber(sequence);

            var invoice = new Invoice
            {
                OrderHeaderId = orderId,
                InvoiceNumber = invoiceNumber,
                KID = kidNumber,
                IssueDate = DateOnly.FromDateTime(DateTime.UtcNow),
                DueDate = order.PaymentDueDate ?? SD.GetDeferredPaymentDueDate(order.OrderDate),
                Currency = order.Currency ?? "NOK",
                Status = InvoiceStatus.Draft,

                // Seller snapshot
                SellerName = _cartivaContact.Name,
                SellerOrgNumber = _cartivaContact.OrgNumber,
                SellerAddress = _cartivaContact.Address,
                SellerEmail = _cartivaContact.Email,
                SellerPhone = _cartivaContact.Phone,

                // Customer snapshot
                CustomerName = order.ApplicationUser?.Company?.Name ?? order.Name,
                CustomerOrgNumber = null, // Add to Company model if needed
                CustomerAddress = $"{order.StreetAddress}, {order.PostalCode} {order.City}",
                CustomerEmail = order.ApplicationUser?.Email,

                // Bank info
                BankAccountNumber = _configuration["Invoice:BankAccount"],
                IBAN = _configuration["Invoice:IBAN"],
                BIC = _configuration["Invoice:BIC"]
            };

            // Create invoice lines from order details with proper VAT data
            int sortOrder = 1;
            foreach (var orderDetail in order.OrderDetails)
            {
                // InvoiceLine.FromOrderDetail now pulls VAT data from OrderDetail
                var line = InvoiceLine.FromOrderDetail(orderDetail);
                line.SortOrder = sortOrder++;
                invoice.Lines.Add(line);
            }

            // Calculate totals from lines (which now have proper VAT breakdown)
            invoice.NetAmount = invoice.Lines.Sum(l => l.LineNetAmount);
            invoice.VatAmount = invoice.Lines.Sum(l => l.LineVatAmount);
            invoice.TotalAmount = invoice.Lines.Sum(l => l.LineTotalAmount);

            // Verify totals match order (with tolerance for rounding)
            var orderTotalCheck = order.SubtotalExVat + order.TotalVatAmount;
            if (Math.Abs(invoice.TotalAmount - order.OrderTotal) > 0.01m && order.OrderTotal > 0)
            {
                _logger.LogWarning(
                    "Invoice total {InvoiceTotal} differs from order total {OrderTotal} for Order {OrderId}. Using order values.",
                    invoice.TotalAmount, order.OrderTotal, orderId);

                // Use order totals if there's a significant discrepancy
                if (order.SubtotalExVat > 0)
                {
                    invoice.NetAmount = order.SubtotalExVat;
                    invoice.VatAmount = order.TotalVatAmount;
                    invoice.TotalAmount = order.OrderTotal;
                }
            }

            _db.Set<Invoice>().Add(invoice);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Generated invoice {InvoiceNumber} for Order {OrderId}", invoiceNumber, orderId);

            // Send invoice generated notification
            if (order.ApplicationUser?.Email != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _notificationService.SendAsync(new NotificationRequest(
                            Recipient: order.ApplicationUser.Email,
                            Type: NotificationType.InvoiceGenerated,
                            TemplateData: new Dictionary<string, object>
                            {
                                ["invoiceNumber"] = invoiceNumber,
                                ["orderNumber"] = orderId.ToString(),
                                ["totalAmount"] = invoice.TotalAmount.ToString("C"),
                                ["dueDate"] = invoice.DueDate.ToString("yyyy-MM-dd"),
                                ["customerName"] = invoice.CustomerName ?? order.Name
                            },
                            UserId: order.ApplicationUserId,
                            ReferenceId: invoice.Id.ToString(),
                            ReferenceType: "Invoice",
                            Subject: $"Invoice {invoiceNumber} - Order #{orderId}"
                        ));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send invoice notification for invoice {InvoiceNumber}", invoiceNumber);
                    }
                });
            }

            return invoice;
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int invoiceId, CancellationToken ct = default)
        {
            return await _db.Set<Invoice>()
                .Include(i => i.Lines)
                .Include(i => i.Payments)
                .Include(i => i.CreditNotes)
                .Include(i => i.OrderHeader)
                    .ThenInclude(o => o!.ApplicationUser)
                .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);
        }

        public async Task<Invoice?> GetInvoiceByOrderIdAsync(int orderId, CancellationToken ct = default)
        {
            return await _db.Set<Invoice>()
                .Include(i => i.Lines)
                .Include(i => i.Payments)
                .Include(i => i.CreditNotes)
                .FirstOrDefaultAsync(i => i.OrderHeaderId == orderId, ct);
        }

        public async Task<bool> MarkInvoiceAsSentAsync(int invoiceId, CancellationToken ct = default)
        {
            var invoice = await _db.Set<Invoice>().FindAsync(new object[] { invoiceId }, ct);
            if (invoice == null) return false;

            if (invoice.Status == InvoiceStatus.Draft)
            {
                invoice.Status = InvoiceStatus.Issued;
            }

            invoice.Status = InvoiceStatus.Sent;
            invoice.SentDate = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Invoice {InvoiceId} marked as sent", invoiceId);
            return true;
        }

        public async Task<InvoicePayment> RecordPaymentAsync(
            int invoiceId,
            decimal amount,
            PaymentMethod paymentMethod,
            string? transactionId = null,
            string? paymentReference = null,
            string? registeredBy = null,
            CancellationToken ct = default)
        {
            var invoice = await _db.Set<Invoice>()
                .Include(i => i.Payments)
                .Include(i => i.CreditNotes)
                .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

            if (invoice == null)
                throw new InvalidOperationException($"Invoice with ID {invoiceId} not found.");

            if (invoice.Status == InvoiceStatus.Cancelled)
                throw new InvalidOperationException("Cannot record payment on a cancelled invoice.");

            var idempotencyKey = InvoicePayment.GenerateIdempotencyKey(invoiceId, transactionId);

            // Check for duplicate payment
            var existingPayment = await _db.Set<InvoicePayment>()
                .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct);

            if (existingPayment != null)
            {
                _logger.LogWarning("Duplicate payment detected for Invoice {InvoiceId} with key {Key}", invoiceId, idempotencyKey);
                return existingPayment;
            }

            var payment = new InvoicePayment
            {
                InvoiceId = invoiceId,
                Amount = amount,
                PaymentDate = DateTime.UtcNow,
                PaymentMethod = paymentMethod,
                TransactionId = transactionId,
                PaymentReference = paymentReference ?? invoice.KID,
                IdempotencyKey = idempotencyKey,
                RegisteredBy = registeredBy
            };

            _db.Set<InvoicePayment>().Add(payment);

            // Recalculate invoice status
            invoice.RecalculateStatus();
            invoice.UpdatedAt = DateTime.UtcNow;

            // Update order payment status if fully paid
            if (invoice.IsFullyPaid && invoice.OrderHeaderId.HasValue)
            {
                var order = await _db.OrderHeaders.FindAsync(new object[] { invoice.OrderHeaderId.Value }, ct);
                if (order != null)
                {
                    order.PaymentStatus = SD.PaymentStatusPaid;
                    order.PaymentDate = DateTime.UtcNow;
                }
            }

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Recorded payment of {Amount} NOK for Invoice {InvoiceId}", amount, invoiceId);

            return payment;
        }

        public async Task<bool> CancelInvoiceAsync(int invoiceId, string cancelledBy, string? reason = null, CancellationToken ct = default)
        {
            var invoice = await _db.Set<Invoice>().FindAsync(new object[] { invoiceId }, ct);
            if (invoice == null) return false;

            if (invoice.Status == InvoiceStatus.Paid)
                throw new InvalidOperationException("Cannot cancel a fully paid invoice. Create a credit note instead.");

            invoice.Cancel(cancelledBy, reason);
            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("Invoice {InvoiceId} cancelled by {User}", invoiceId, cancelledBy);
            return true;
        }

        public async Task<List<Invoice>> GetOverdueInvoicesAsync(CancellationToken ct = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            return await _db.Set<Invoice>()
                .Include(i => i.OrderHeader)
                    .ThenInclude(o => o!.ApplicationUser)
                        .ThenInclude(u => u!.Company)
                .Where(i => i.DueDate < today &&
                           i.Status != InvoiceStatus.Paid &&
                           i.Status != InvoiceStatus.Cancelled)
                .OrderBy(i => i.DueDate)
                .ToListAsync(ct);
        }

        public async Task<List<Invoice>> GetInvoicesDueSoonAsync(int daysUntilDue, CancellationToken ct = default)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var targetDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(daysUntilDue));

            return await _db.Set<Invoice>()
                .Include(i => i.OrderHeader)
                    .ThenInclude(o => o!.ApplicationUser)
                .Where(i => i.DueDate >= today &&
                           i.DueDate <= targetDate &&
                           i.Status != InvoiceStatus.Paid &&
                           i.Status != InvoiceStatus.Cancelled)
                .OrderBy(i => i.DueDate)
                .ToListAsync(ct);
        }

        public async Task RefreshInvoiceStatusAsync(int invoiceId, CancellationToken ct = default)
        {
            var invoice = await _db.Set<Invoice>()
                .Include(i => i.Payments)
                .Include(i => i.CreditNotes)
                .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

            if (invoice == null) return;

            invoice.RecalculateStatus();
            invoice.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
        }

        public async Task<int> GetNextInvoiceSequenceAsync(CancellationToken ct = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"{SD.InvoiceNumberPrefix}-{year}-";

            var lastInvoice = await _db.Set<Invoice>()
                .Where(i => i.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(i => i.InvoiceNumber)
                .FirstOrDefaultAsync(ct);

            if (lastInvoice == null)
                return 1;

            var lastNumberStr = lastInvoice.InvoiceNumber.Replace(prefix, "");
            if (int.TryParse(lastNumberStr, out int lastNumber))
                return lastNumber + 1;

            return 1;
        }

        public async Task<int> GetNextCreditNoteSequenceAsync(CancellationToken ct = default)
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"{SD.CreditNoteNumberPrefix}-{year}-";

            var lastCreditNote = await _db.Set<CreditNote>()
                .Where(c => c.CreditNoteNumber.StartsWith(prefix))
                .OrderByDescending(c => c.CreditNoteNumber)
                .FirstOrDefaultAsync(ct);

            if (lastCreditNote == null)
                return 1;

            var lastNumberStr = lastCreditNote.CreditNoteNumber.Replace(prefix, "");
            if (int.TryParse(lastNumberStr, out int lastNumber))
                return lastNumber + 1;

            return 1;
        }
    }
}
