using System;
using System.Collections.Generic;
using System.Text;

namespace Cartiva.Domain.ViewModels
{
    public class InvoiceDashboardViewModel
    {
        // New Invoice entity collections (primary)
        public List<Invoice> Invoices { get; set; } = new();
        public List<Invoice> OverdueInvoiceEntities { get; set; } = new();
        public List<Invoice> PendingInvoiceEntities { get; set; } = new();
        public List<Invoice> PaidInvoiceEntities { get; set; } = new();

        // Legacy OrderHeader collections (for backward compatibility with orders without Invoice records)
        public List<OrderHeader> OverdueInvoices { get; set; } = new();
        public List<OrderHeader> PendingInvoices { get; set; } = new();
        public List<OrderHeader> PaidInvoices { get; set; } = new();

        // Helper to get order total (uses OrderTotal directly)
        private static decimal GetOrderTotal(OrderHeader o) => o.OrderTotal;

        // Helper to get ex VAT (fallback to calculation if not set)
        private static decimal GetOrderExVat(OrderHeader o) => 
            o.SubtotalExVat > 0 ? o.SubtotalExVat : o.OrderTotal / 1.25m;

        // Helper to get VAT amount (fallback to calculation if not set)
        private static decimal GetOrderVat(OrderHeader o) => 
            o.TotalVatAmount > 0 ? o.TotalVatAmount : o.OrderTotal - (o.OrderTotal / 1.25m);

        // =========================
        // OVERDUE TOTALS
        // =========================
        public decimal TotalOverdue => 
            OverdueInvoiceEntities.Sum(i => i.RemainingAmount) +
            OverdueInvoices.Sum(GetOrderTotal);

        public decimal TotalOverdueExVat =>
            OverdueInvoiceEntities.Sum(i => i.NetAmount) +
            OverdueInvoices.Sum(GetOrderExVat);

        public decimal TotalOverdueVat =>
            OverdueInvoiceEntities.Sum(i => i.VatAmount) +
            OverdueInvoices.Sum(GetOrderVat);

        // =========================
        // PENDING TOTALS
        // =========================
        public decimal TotalPending =>
            PendingInvoiceEntities.Sum(i => i.RemainingAmount) +
            PendingInvoices.Sum(GetOrderTotal);

        public decimal TotalPendingExVat =>
            PendingInvoiceEntities.Sum(i => i.NetAmount) +
            PendingInvoices.Sum(GetOrderExVat);

        public decimal TotalPendingVat =>
            PendingInvoiceEntities.Sum(i => i.VatAmount) +
            PendingInvoices.Sum(GetOrderVat);

        // =========================
        // PAID TOTALS
        // =========================
        public decimal TotalPaid =>
            PaidInvoiceEntities.Sum(i => i.TotalAmount) +
            PaidInvoices.Sum(GetOrderTotal);

        public decimal TotalPaidExVat =>
            PaidInvoiceEntities.Sum(i => i.NetAmount) +
            PaidInvoices.Sum(GetOrderExVat);

        public decimal TotalPaidVat =>
            PaidInvoiceEntities.Sum(i => i.VatAmount) +
            PaidInvoices.Sum(GetOrderVat);

        // =========================
        // OUTSTANDING TOTALS (Overdue + Pending)
        // =========================
        public decimal TotalOutstanding => TotalOverdue + TotalPending;

        public decimal TotalOutstandingExVat => TotalOverdueExVat + TotalPendingExVat;

        public decimal TotalOutstandingVat => TotalOverdueVat + TotalPendingVat;

        // =========================
        // COUNTS
        // =========================
        public int OverdueCount => OverdueInvoiceEntities.Count + OverdueInvoices.Count;
        public int PendingCount => PendingInvoiceEntities.Count + PendingInvoices.Count;
        public int PaidCount => PaidInvoiceEntities.Count + PaidInvoices.Count;
        public int TotalCount => OverdueCount + PendingCount + PaidCount;

        // Check if using new invoice system
        public bool HasInvoiceEntities => Invoices.Any() || OverdueInvoiceEntities.Any() || 
                                          PendingInvoiceEntities.Any() || PaidInvoiceEntities.Any();
    }
}
