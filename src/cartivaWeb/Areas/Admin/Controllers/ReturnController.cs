using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Domain.Enums;
using Cartiva.Persistence;
using Cartiva.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public class ReturnController : Controller
    {
        private readonly IReturnService _returnService;
        private readonly ICreditNoteService _creditNoteService;
        private readonly IAccountsReceivableAdjustmentService _arAdjustmentService;
        private readonly ApplicationDbContext _db;

        public ReturnController(
            IReturnService returnService,
            ICreditNoteService creditNoteService,
            IAccountsReceivableAdjustmentService arAdjustmentService,
            ApplicationDbContext db)
        {
            _returnService = returnService;
            _creditNoteService = creditNoteService;
            _arAdjustmentService = arAdjustmentService;
            _db = db;
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
        // CREATE AR ADJUSTMENT FROM APPROVED RETURN
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateARAdjustment(int id)
        {
            try
            {
                // Load return request with OrderDetail -> OrderHeader -> ApplicationUser
                var returnRequest = await _db.ReturnRequests
                    .Include(r => r.OrderDetail)
                        .ThenInclude(od => od.OrderHeader)
                            .ThenInclude(oh => oh.ApplicationUser) // get user who has CompanyId
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (returnRequest == null)
                {
                    TempData["error"] = "Return request not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (returnRequest.Status != ReturnStatus.Approved)
                {
                    TempData["error"] = "Only approved returns can create an AR adjustment.";
                    return RedirectToAction(nameof(Index));
                }

                var orderHeader = returnRequest.OrderDetail?.OrderHeader;
                if (orderHeader == null)
                {
                    TempData["error"] = "Could not find order header for this return.";
                    return RedirectToAction(nameof(Index));
                }

                // Get company ID from the ApplicationUser associated with this order
                var applicationUser = orderHeader.ApplicationUser;
                if (applicationUser == null || !applicationUser.CompanyId.HasValue)
                {
                    TempData["error"] = "No active company associated with this order.";
                    return RedirectToAction(nameof(Index));
                }

                var companyId = applicationUser.CompanyId.Value;

                // Find the invoice associated with this order header
                var invoice = await _db.Invoices
                    .FirstOrDefaultAsync(i => i.OrderHeaderId == orderHeader.Id);

                if (invoice == null)
                {
                    TempData["error"] = "No invoice found for the associated order.";
                    return RedirectToAction(nameof(Index));
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var adjustment = await _arAdjustmentService.CreateFromReturnRequestAsync(
                    returnRequestId: id,
                    invoiceId: invoice.Id,
                    companyId: companyId,
                    createdByUserId: userId
                );

                TempData["success"] = $"AR Adjustment #{adjustment.Id} created successfully. You can now apply the Stripe credit balance from the AR Adjustment list.";
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Failed to create AR adjustment: {ex.Message}";
            }

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
        // APPLY AR ADJUSTMENT (marks return as complete) – kept for backward compatibility
        // =========================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyARAdjustment(int id)
        {
            try
            {
                var returnRequest = await _returnService.GetReturnRequestByIdAsync(id);
                if (returnRequest == null)
                {
                    TempData["error"] = "Return request not found.";
                    return RedirectToAction(nameof(Index));
                }

                if (returnRequest.Status != ReturnStatus.Approved)
                {
                    TempData["error"] = "Only approved returns can be finalized.";
                    return RedirectToAction(nameof(Index));
                }

                var result = await _returnService.FinalizeARAdjustmentReturnAsync(id);
                TempData[result.Success ? "success" : "error"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Error applying AR adjustment: {ex.Message}";
            }

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
                return RedirectToAction("Details", "CreditNote", new { area = "Admin", id = creditNote.Id });
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}