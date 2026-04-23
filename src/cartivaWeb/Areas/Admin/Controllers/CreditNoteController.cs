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
        public async Task<IActionResult> Index(int? invoiceId)
        {
            if (invoiceId == null)
            {
                // later you can replace with "GetAll"
                return View(new List<CreditNote>());
            }

            var list = await _creditNoteService
                .GetCreditNotesForInvoiceAsync(invoiceId.Value);

            return View(list);
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