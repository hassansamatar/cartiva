using System;
using System.Collections.Generic;
using System.Text;

namespace Cartiva.Domain.ViewModels
{
    public class InvoiceDashboardViewModel
    {
        public List<OrderHeader> OverdueInvoices { get; set; } = new();
        public List<OrderHeader> PendingInvoices { get; set; } = new();
        public List<OrderHeader> PaidInvoices { get; set; } = new();
    }
}
