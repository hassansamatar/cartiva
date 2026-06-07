using Cartiva.Domain;
using Cartiva.Domain.Enums;
using Cartiva.Domain.ViewModels;
using Cartiva.Persistence;
using Cartiva.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text.Json;

namespace cartivaWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
public class RevenueController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RevenueController> _logger;

    public RevenueController(
        ApplicationDbContext db,
        ILogger<RevenueController> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var culture = CultureInfo.GetCultureInfo("nb-NO");
            var now = DateTime.UtcNow;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var lastMonthEnd = thisMonthStart.AddDays(-1);
            var thisYearStart = new DateTime(now.Year, 1, 1);

            // Get all orders
            var allOrders = await _db.OrderHeaders
                .Where(o => o.OrderStatus != OrderStatus.Cancelled)
                .ToListAsync();

            var totalOrders = allOrders.Count;
            var totalOrdersRevenue = allOrders.Sum(o => o.OrderTotal);
            var averageOrderValue = totalOrders > 0 ? totalOrdersRevenue / totalOrders : 0;

            // Get revenue by month
            var revenueThisMonth = allOrders
                .Where(o => o.OrderDate.Date >= thisMonthStart.Date && o.OrderDate.Date <= now.Date)
                .Sum(o => o.OrderTotal);

            var revenueLastMonth = allOrders
                .Where(o => o.OrderDate.Date >= lastMonthStart.Date && o.OrderDate.Date <= lastMonthEnd.Date)
                .Sum(o => o.OrderTotal);

            var revenueThisYear = allOrders
                .Where(o => o.OrderDate.Date >= thisYearStart.Date && o.OrderDate.Date <= now.Date)
                .Sum(o => o.OrderTotal);

            // Get all invoices
            var allInvoices = await _db.Invoices
                .Where(i => i.Status != InvoiceStatus.Cancelled)
                .ToListAsync();

            var totalInvoices = allInvoices.Count;
            var totalInvoicesOutstanding = allInvoices.Sum(i => i.RemainingAmount);
            var averageInvoiceValue = totalInvoices > 0 ? allInvoices.Sum(i => i.TotalAmount) / totalInvoices : 0;

            // Tax metrics
            var totalTaxCollected = allInvoices.Sum(i => i.VatAmount);
            var taxThisMonth = allInvoices
                .Where(i => i.IssueDate >= DateOnly.FromDateTime(thisMonthStart) && 
                       i.IssueDate <= DateOnly.FromDateTime(now))
                .Sum(i => i.VatAmount);

            // Net Revenue (Total Revenue - Tax - Credits - Adjustments)
            var creditNotes = await _db.CreditNotes
                .Where(c => c.Status != CreditNoteStatus.Cancelled)
                .ToListAsync();
            var totalCreditNotes = creditNotes.Count;
            var creditNotesAmount = creditNotes.Sum(c => c.TotalAmount);

            var arAdjustments = await _db.AccountsReceivableAdjustments
                .Where(a => a.Status == ARAdjustmentStatus.Applied)
                .ToListAsync();
            var totalARAdjustments = arAdjustments.Count;
            var arAdjustmentsAmount = arAdjustments.Sum(a => Math.Abs(a.Amount));

            var netRevenue = totalOrdersRevenue - totalTaxCollected - creditNotesAmount - arAdjustmentsAmount;
            var netRevenueThisMonth = revenueThisMonth - taxThisMonth;

            // Payment metrics
            var overdueInvoices = allInvoices.Where(i => i.IsOverdue).ToList();
            var overdueInvoicesCount = overdueInvoices.Count;
            var overdueAmount = overdueInvoices.Sum(i => i.RemainingAmount);

            var paidAmount = allInvoices.Sum(i => i.TotalPaid);
            var pendingAmount = allInvoices
                .Where(i => i.Status == InvoiceStatus.PartiallyPaid || i.Status == InvoiceStatus.Overdue)
                .Sum(i => i.RemainingAmount);

            // Percentages
            var paidPercentage = totalOrdersRevenue > 0 ? (paidAmount / totalOrdersRevenue) * 100 : 0;
            var overduePercentage = totalOrdersRevenue > 0 ? (overdueAmount / totalOrdersRevenue) * 100 : 0;
            var taxPercentage = totalOrdersRevenue > 0 ? (totalTaxCollected / totalOrdersRevenue) * 100 : 0;

            // Chart Data - Revenue by Status
            var revenueByStatus = new
            {
                labels = new[] { "Paid", "Pending", "Overdue" },
                data = new[] { paidAmount, pendingAmount, overdueAmount },
                colors = new[] { "#28a745", "#ffc107", "#dc3545" }
            };

            // Chart Data - Revenue Breakdown
            var revenueBreakdown = new
            {
                labels = new[] { "Gross Revenue", "Tax", "Credit Notes", "AR Adjustments", "Net Revenue" },
                data = new[] { totalOrdersRevenue, totalTaxCollected, creditNotesAmount, arAdjustmentsAmount, netRevenue },
                colors = new[] { "#0d6efd", "#fd7e14", "#dc3545", "#6f42c1", "#198754" }
            };

            // Chart Data - Last 12 Months
            var monthlyData = new List<(string Month, decimal Revenue)>();
            for (int i = 11; i >= 0; i--)
            {
                var monthDate = now.AddMonths(-i);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                var monthRevenue = allOrders
                    .Where(o => o.OrderDate >= monthStart && o.OrderDate <= monthEnd)
                    .Sum(o => o.OrderTotal);
                monthlyData.Add((monthStart.ToString("MMM"), monthRevenue));
            }

            var monthlyTrend = new
            {
                labels = monthlyData.Select(m => m.Month).ToArray(),
                data = monthlyData.Select(m => m.Revenue).ToArray()
            };

            var model = new RevenueDashboardViewModel
            {
                TotalRevenue = totalOrdersRevenue,
                TotalOrdersRevenue = totalOrdersRevenue,
                TotalInvoicesOutstanding = totalInvoicesOutstanding,
                NetRevenue = netRevenue,
                TotalTaxCollected = totalTaxCollected,
                TotalOrders = totalOrders,
                TotalInvoices = totalInvoices,
                OverdueInvoicesCount = overdueInvoicesCount,
                TotalCreditNotes = totalCreditNotes,
                TotalARAdjustments = totalARAdjustments,
                OverdueAmount = overdueAmount,
                PaidAmount = paidAmount,
                PendingAmount = pendingAmount,
                CreditNotesAmount = creditNotesAmount,
                ARAdjustmentsAmount = arAdjustmentsAmount,
                RevenueThisMonth = revenueThisMonth,
                RevenueLastMonth = revenueLastMonth,
                RevenueThisYear = revenueThisYear,
                NetRevenueThisMonth = netRevenueThisMonth,
                TaxThisMonth = taxThisMonth,
                AverageOrderValue = averageOrderValue,
                AverageInvoiceValue = averageInvoiceValue,
                RevenueByStatusJson = JsonSerializer.Serialize(revenueByStatus),
                RevenueBreakdownJson = JsonSerializer.Serialize(revenueBreakdown),
                MonthlyTrendJson = JsonSerializer.Serialize(monthlyTrend),
                PaidPercentage = paidPercentage,
                OverduePercentage = overduePercentage,
                TaxPercentage = taxPercentage
            };

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading revenue dashboard");
            TempData["Error"] = "Error loading revenue data";
            return View(new RevenueDashboardViewModel());
        }
    }

    public async Task<IActionResult> Monthly()
    {
        try
        {
            var lastTwelveMonths = Enumerable.Range(0, 12)
                .Select(i => DateTime.UtcNow.AddMonths(-i))
                .OrderBy(d => d)
                .Select(d => new DateTime(d.Year, d.Month, 1))
                .ToList();

            var monthlyData = new List<(string Month, decimal Revenue, decimal Tax, decimal NetRevenue, decimal Credits, decimal Adjustments)>();

            foreach (var month in lastTwelveMonths)
            {
                var monthEnd = month.AddMonths(1).AddDays(-1);

                var monthRevenue = await _db.OrderHeaders
                    .Where(o => o.OrderDate >= month && o.OrderDate <= monthEnd && o.OrderStatus != OrderStatus.Cancelled)
                    .SumAsync(o => o.OrderTotal);

                var monthTax = await _db.Invoices
                    .Where(i => i.IssueDate >= DateOnly.FromDateTime(month) && 
                           i.IssueDate <= DateOnly.FromDateTime(monthEnd) && 
                           i.Status != InvoiceStatus.Cancelled)
                    .SumAsync(i => i.VatAmount);

                var monthCredits = await _db.CreditNotes
                    .Where(c => c.CreatedAt >= month && c.CreatedAt <= monthEnd && 
                           c.Status != CreditNoteStatus.Cancelled)
                    .SumAsync(c => c.TotalAmount);

                var monthAdjustments = await _db.AccountsReceivableAdjustments
                    .Where(a => a.CreatedAt >= month && a.CreatedAt <= monthEnd && 
                           a.Status == ARAdjustmentStatus.Applied)
                    .SumAsync(a => Math.Abs(a.Amount));

                var netRevenue = monthRevenue - monthTax - monthCredits - monthAdjustments;

                monthlyData.Add((month.ToString("MMM yyyy"), monthRevenue, monthTax, netRevenue, monthCredits, monthAdjustments));
            }

            ViewBag.MonthlyData = monthlyData;
            return View(monthlyData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading monthly revenue data");
            TempData["Error"] = "Error loading monthly data";
            return View(new List<(string, decimal, decimal, decimal, decimal, decimal)>());
        }
    }
}
