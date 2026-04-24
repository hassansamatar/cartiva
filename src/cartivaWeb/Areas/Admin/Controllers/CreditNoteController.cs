using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace cartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class CreditNoteController : Controller
    {
        private readonly ICreditNoteService _creditNoteService;

        public CreditNoteController(ICreditNoteService creditNoteService)
        {
            _creditNoteService = creditNoteService;
        }

        // GET: Admin/CreditNote
        public async Task<IActionResult> Index(int? invoiceId, int? orderId, string? search, string? status, string? type)
        {
            var creditNotes = invoiceId == null
                ? await _creditNoteService.GetAllCreditNotesAsync()
                : await _creditNoteService.GetCreditNotesForInvoiceAsync(invoiceId.Value);

            if (orderId.HasValue)
            {
                creditNotes = creditNotes.Where(c =>
                        c.OriginalInvoice?.OrderHeaderId == orderId.Value ||
                        c.ReturnRequest?.OrderDetail?.OrderHeaderId == orderId.Value)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = search.Trim();
                creditNotes = creditNotes.Where(c =>
                        c.CreditNoteNumber.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                        c.CustomerName.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                        c.Reason.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                        (c.OriginalInvoice != null && c.OriginalInvoice.InvoiceNumber.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase)) ||
                        (c.OriginalInvoice?.OrderHeaderId?.ToString() ?? string.Empty).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                        (c.ReturnRequestId?.ToString() ?? string.Empty).Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<CreditNoteStatus>(status, true, out var parsedStatus))
            {
                creditNotes = creditNotes.Where(c => c.Status == parsedStatus).ToList();
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                creditNotes = type switch
                {
                    "return" => creditNotes.Where(c => c.ReturnRequestId.HasValue).ToList(),
                    "cancellation" => creditNotes.Where(c => !c.ReturnRequestId.HasValue).ToList(),
                    _ => creditNotes
                };
            }

            ViewBag.CurrentSearch = search;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentType = type;
            ViewBag.CurrentOrderId = orderId;

            return View(creditNotes);
        }

        // GET: Admin/CreditNote/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var creditNote = await _creditNoteService.GetCreditNoteByIdAsync(id);

            if (creditNote == null)
                return NotFound();

            return View(creditNote);
        }
        public async Task<IActionResult> DetailsByReturn(int returnRequestId)
        {
            var creditNote = await _creditNoteService
                .GetCreditNoteByReturnRequestIdAsync(returnRequestId);

            if (creditNote == null)
            {
                TempData["error"] = "Credit note not found.";
                return RedirectToAction("Index", "Return");
            }

            return View("Details", creditNote);
        }
    } }