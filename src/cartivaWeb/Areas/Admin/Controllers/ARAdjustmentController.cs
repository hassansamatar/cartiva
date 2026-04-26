using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Domain.Enums;
using Cartiva.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

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

        // GET: Admin/ARAdjustment/Create
        // DISABLED: Manual AR Adjustment creation not allowed
        // AR Adjustments are created automatically from return approvals
        /*
        public async Task<IActionResult> Create(int? companyId, int? invoiceId)
        {
            var companies = await _companyService.GetAllCompaniesAsync();
            ViewBag.Companies = new SelectList(
                companies.Where(c => c.IsActive).OrderBy(c => c.Name),
                "Id",
                "Name",
                companyId);

            if (invoiceId.HasValue)
            {
                var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId.Value);
                if (invoice != null)
                {
                    ViewBag.SelectedInvoice = invoice;
                }
            }

            return View();
        }
        */

        // POST: Admin/ARAdjustment/Create
        // DISABLED: Manual AR Adjustment creation not allowed
        /*
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            int companyId,
            int invoiceId,
            decimal amount,
            string reason,
            string? notes)
        {
            try
            {
                var adjustment = await _arAdjustmentService.CreateManualAdjustmentAsync(
                    companyId: companyId,
                    invoiceId: invoiceId,
                    amount: amount,
                    reason: reason,
                    notes: notes,
                    createdByUserId: User.Identity?.Name);

                TempData["success"] = "AR Adjustment created successfully.";
                return RedirectToAction(nameof(Details), new { id = adjustment.Id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;

                // Reload view with data
                var companies = await _companyService.GetAllCompaniesAsync();
                ViewBag.Companies = new SelectList(
                    companies.Where(c => c.IsActive).OrderBy(c => c.Name),
                    "Id",
                    "Name",
                    companyId);

                var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId);
                if (invoice != null)
                {
                    ViewBag.SelectedInvoice = invoice;
                }

                return View();
            }
        }
        */

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
    }
}
