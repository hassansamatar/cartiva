using Cartiva.Domain;
using Cartiva.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Cartiva.Infrastructure.EmailServices
{
    public class OverdueInvoiceService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<OverdueInvoiceService> _logger;

        public OverdueInvoiceService(ApplicationDbContext db, ILogger<OverdueInvoiceService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // Mark all overdue invoices as sent
        public async Task SendOverdueInvoicesAsync(CancellationToken cancellationToken)
        {
            var overdueOrders = await _db.OrderHeaders
                .Include(o => o.ApplicationUser)
                .Where(o => o.PaymentStatus == "Deferred" &&
                            o.PaymentDueDate < DateOnly.FromDateTime(DateTime.Now) &&
                            !o.InvoiceSent &&
                            o.ApplicationUser.CompanyId != null)
                .ToListAsync(cancellationToken);

            foreach (var order in overdueOrders)
            {
                if (order.InvoiceSent) continue;
                try
                {
                    await MarkInvoiceAsSentForOrderAsync(order, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to mark invoice as sent for order {order.Id}");
                }
            }
        }

        // Mark a single overdue invoice as sent
        public async Task MarkInvoiceAsSentForOrderAsync(OrderHeader order, CancellationToken cancellationToken)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));
            if (order.InvoiceSent) return;

            order.InvoiceSent = true;
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation($"Invoice marked as sent for overdue order {order.Id}");
        }
    }
}