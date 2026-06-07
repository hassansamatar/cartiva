namespace Cartiva.Domain.ViewModels;

public class RevenueDashboardViewModel
{
    // Core Revenue Metrics
    public decimal TotalRevenue { get; set; }
    public decimal TotalOrdersRevenue { get; set; }
    public decimal TotalInvoicesOutstanding { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal TotalTaxCollected { get; set; }

    // Counts
    public int TotalOrders { get; set; }
    public int TotalInvoices { get; set; }
    public int OverdueInvoicesCount { get; set; }
    public int TotalCreditNotes { get; set; }
    public int TotalARAdjustments { get; set; }

    // Payment & Amount Metrics
    public decimal OverdueAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal CreditNotesAmount { get; set; }
    public decimal ARAdjustmentsAmount { get; set; }

    // Time-based metrics
    public decimal RevenueThisMonth { get; set; }
    public decimal RevenueLastMonth { get; set; }
    public decimal RevenueThisYear { get; set; }
    public decimal NetRevenueThisMonth { get; set; }
    public decimal TaxThisMonth { get; set; }

    // Average metrics
    public decimal AverageOrderValue { get; set; }
    public decimal AverageInvoiceValue { get; set; }

    // Chart Data (JSON strings)
    public string? RevenueByStatusJson { get; set; }
    public string? RevenueBreakdownJson { get; set; }
    public string? MonthlyTrendJson { get; set; }

    // Percentages
    public decimal PaidPercentage { get; set; }
    public decimal OverduePercentage { get; set; }
    public decimal TaxPercentage { get; set; }
}
