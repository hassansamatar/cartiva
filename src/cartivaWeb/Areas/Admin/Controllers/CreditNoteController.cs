using Cartiva.Application.Abstractions;
using Cartiva.Domain.Interfaces;
using Cartiva.Domain.Enums;
using Cartiva.Domain;
using Cartiva.Domain.Enums;
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
    public class CreditNoteController : Controller
    {
        private readonly ICreditNoteService _creditNoteService;
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<CreditNoteController> _logger;

        public CreditNoteController(
            ICreditNoteService creditNoteService,
            INotificationService notificationService,
            ApplicationDbContext db,
            ILogger<CreditNoteController> logger)
        {
            _creditNoteService = creditNoteService;
            _notificationService = notificationService;
            _db = db;
            _logger = logger;
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

        // POST: Admin/CreditNote/SendEmail/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendEmail(int id)
        {
            var creditNote = await _db.CreditNotes
                .Include(cn => cn.OriginalInvoice)
                    .ThenInclude(i => i.OrderHeader)
                        .ThenInclude(o => o!.ApplicationUser)
                .Include(cn => cn.ReturnRequest)
                    .ThenInclude(rr => rr!.OrderDetail)
                        .ThenInclude(od => od.OrderHeader)
                            .ThenInclude(oh => oh.ApplicationUser)
                .FirstOrDefaultAsync(cn => cn.Id == id);

            if (creditNote == null)
            {
                TempData["Error"] = "Credit note not found.";
                return RedirectToAction(nameof(Index));
            }

            // Get email from the ApplicationUser who placed the order
            var customerEmail = creditNote.OriginalInvoice?.OrderHeader?.ApplicationUser?.Email
                ?? creditNote.OriginalInvoice?.CustomerEmail
                ?? creditNote.ReturnRequest?.OrderDetail?.OrderHeader?.ApplicationUser?.Email;

            if (string.IsNullOrWhiteSpace(customerEmail))
            {
                TempData["Error"] = "Cannot send email: No customer email address found.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                await _notificationService.SendAsync(new NotificationRequest(
                    Recipient: customerEmail,
                    Type: NotificationType.CreditNoteGenerated,
                    TemplateData: new Dictionary<string, object>
                    {
                        ["creditNoteId"] = creditNote.Id.ToString(),
                        ["creditNoteNumber"] = creditNote.CreditNoteNumber,
                        ["orderId"] = creditNote.OriginalInvoice?.OrderHeaderId?.ToString() ?? string.Empty,
                        ["issueDate"] = creditNote.IssueDate.ToString("dd MMM yyyy"),
                        ["totalAmount"] = creditNote.TotalAmount.ToString("N2", CultureInfo.GetCultureInfo("nb-NO")),
                        ["netAmount"] = creditNote.NetAmount.ToString("N2", CultureInfo.GetCultureInfo("nb-NO")),
                        ["vatAmount"] = creditNote.VatAmount.ToString("N2", CultureInfo.GetCultureInfo("nb-NO")),
                        ["currency"] = creditNote.Currency,
                        ["status"] = creditNote.Status.ToString(),
                        ["reason"] = creditNote.Reason ?? string.Empty,
                        ["customerName"] = creditNote.CustomerName,
                        ["notes"] = creditNote.Notes ?? string.Empty
                    },
                    UserId: creditNote.OriginalInvoice?.OrderHeader?.ApplicationUserId 
                        ?? creditNote.ReturnRequest?.OrderDetail?.OrderHeader?.ApplicationUserId,
                    ReferenceId: creditNote.Id.ToString(),
                    ReferenceType: "CreditNote",
                    Subject: $"Credit Note {creditNote.CreditNoteNumber} - {creditNote.TotalAmount:N2} {creditNote.Currency}"
                ));

                TempData["Success"] = $"Credit note {creditNote.CreditNoteNumber} sent successfully to {customerEmail}.";
                _logger.LogInformation("Credit note {CreditNoteId} email sent to {Email}", id, customerEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send credit note email for ID {Id}", id);
                TempData["Error"] = "Failed to send email. Please try again.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
