using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Domain.Enums;
using Cartiva.Domain.Interfaces;
using Cartiva.Persistence;
using Cartiva.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace cartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class ARAdjustmentController : Controller
    {
        private readonly IAccountsReceivableAdjustmentService _arAdjustmentService;
        private readonly ICompanyService _companyService;
        private readonly IInvoiceService _invoiceService;

        public ARAdjustmentController(
            IAccountsReceivableAdjustmentService arAdjustmentService,
            ICompanyService companyService,
            IInvoiceService invoiceService)
        {
            _arAdjustmentService = arAdjustmentService;
            _companyService = companyService;
            _invoiceService = invoiceService;
        }

        // GET: Admin/ARAdjustment
        public async Task<IActionResult> Index(
            int? companyId,
            string? status,
            DateTime? fromDate,
            DateTime? toDate,
            string? search)
        {
            ARAdjustmentStatus? statusFilter = null;
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<ARAdjustmentStatus>(status, true, out var parsedStatus))
            {
                statusFilter = parsedStatus;
            }

            var adjustments = await _arAdjustmentService.GetAllAsync(
                companyId: companyId,
                status: statusFilter,
                fromDate: fromDate,
                toDate: toDate);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();
                adjustments = adjustments.Where(a =>
                        a.Company.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                        a.Invoice.InvoiceNumber.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                        a.Reason.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                        a.Id.ToString().Contains(normalizedSearch))
                    .ToList();
            }

            ViewBag.CurrentCompanyId = companyId;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentFromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentSearch = search;

            // Get companies for filter dropdown
            var companies = await _companyService.GetAllCompaniesAsync();
            ViewBag.Companies = companies.Where(c => c.IsActive).OrderBy(c => c.Name).ToList();

            return View(adjustments);
        }

        // GET: Admin/ARAdjustment/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var adjustment = await _arAdjustmentService.GetByIdAsync(id);

            if (adjustment == null)
                return NotFound();

            return View(adjustment);
        }

        // POST: Admin/ARAdjustment/ApplyStripe/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyStripe(int id)
        {
            try
            {
                var success = await _arAdjustmentService.ApplyStripeCreditBalanceAsync(id);

                if (success)
                    TempData["success"] = "Stripe credit balance applied successfully.";
                else
                    TempData["error"] = "Failed to apply Stripe credit balance. Check logs for details.";
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Error: {ex.Message}";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: Admin/ARAdjustment/ConfigureStripeCustomer/{companyId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfigureStripeCustomer(int companyId, string? returnUrl = null)
        {
            try
            {
                var stripeCustomerId = await _arAdjustmentService.ConfigureStripeCustomerAsync(companyId);
                TempData["success"] = $"Stripe Customer configured successfully. ID: {stripeCustomerId}";
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Failed to configure Stripe Customer: {ex.Message}";
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/ARAdjustment/GetInvoicesForCompany?companyId=1
        [HttpGet]
        public async Task<IActionResult> GetInvoicesForCompany(int companyId)
        {
            try
            {
                var invoices = await _invoiceService.GetInvoicesForCompanyAsync(companyId);

                var result = invoices
                    .Where(i => i.RemainingAmount > 0) // Only outstanding invoices
                    .Select(i => new
                    {
                        id = i.Id,
                        invoiceNumber = i.InvoiceNumber,
                        totalAmount = i.TotalAmount,
                        remainingAmount = i.RemainingAmount,
                        currency = i.Currency,
                        dueDate = i.DueDate.ToString("yyyy-MM-dd")
                    })
                    .ToList();

                return Json(result);
            }
            catch
            {
                return Json(new List<object>());
            }
        }

        // POST: Admin/ARAdjustment/SendEmail/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmail(int id)
        {
            var success = await _arAdjustmentService.SendAdjustmentEmailAsync(id);

            if (!success)
            {
                TempData["error"] = "Failed to send email. AR Adjustment not found or no recipient email address.";
                return RedirectToAction(nameof(Index));
            }

            TempData["success"] = "AR Adjustment notification email sent successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}