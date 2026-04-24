using Cartiva.Application.Abstractions;
using Cartiva.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class ReturnController : Controller
    {
        private readonly IReturnService _returnService;
        private readonly ICreditNoteService _creditNoteService;

        public ReturnController(
            IReturnService returnService,
            ICreditNoteService creditNoteService)
        {
            _returnService = returnService;
            _creditNoteService = creditNoteService;
        }

        // GET: Admin/Return
        public async Task<IActionResult> Index()
        {
            var returns = await _returnService.GetAllReturnRequestsAsync();
            return View(returns);
        }

        // =========================
        // APPROVE RETURN
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? adminNote)
        {
            var result = await _returnService.ApproveReturnAsync(id, adminNote);

            TempData[result.Success ? "success" : "error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // REJECT RETURN
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? adminNote)
        {
            var result = await _returnService.RejectReturnAsync(id, adminNote);

            TempData[result.Success ? "success" : "error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // PROCESS REFUND (Stripe + status update)
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Refund(int id)
        {
            var result = await _returnService.ProcessRefundAsync(id);

            TempData[result.Success ? "success" : "error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        // =========================
        // CREATE CREDIT NOTE FROM RETURN
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCreditNote(int id)
        {
            try
            {
                var creditNote = await _creditNoteService.CreateFromReturnRequestAsync(id);

                TempData["success"] = "Credit note created successfully.";

                // 🔥 NAVIGATE TO DETAILS PAGE
                return RedirectToAction(
                    "Details",
                    "CreditNote",
                    new { area = "Admin", id = creditNote.Id }
                );
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}