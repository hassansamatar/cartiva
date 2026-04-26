using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Domain.Enums;
using Cartiva.Domain.Extensions;
using Cartiva.Domain.ViewModels;
using Cartiva.Persistence;
using Cartiva.Shared;
using Cartiva.Shared.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace cartivaWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InvoiceController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly CartivaContact _cartivaContact;
        private readonly IConfiguration _configuration;
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(
            ApplicationDbContext db, 
            CartivaContact cartivaContact, 
            IConfiguration configuration,
            IInvoiceService invoiceService)
        {
            _db = db;
            _cartivaContact = cartivaContact;
            _configuration = configuration;
            _invoiceService = invoiceService;
        }

        // GET: Admin/Invoice/Index
        public async Task<IActionResult> Index()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);
            var viewModel = new InvoiceDashboardViewModel();

            // Get invoices from new Invoice entity
            var allInvoices = await _db.Invoices
                .Include(i => i.OrderHeader)
                    .ThenInclude(o => o!.ApplicationUser)
                        .ThenInclude(u => u!.Company)
                .Include(i => i.Lines)
                .Include(i => i.Payments)
                .ToListAsync();

            viewModel.OverdueInvoiceEntities = allInvoices
                .Where(i => i.Status != Cartiva.Domain.Enums.InvoiceStatus.Paid && 
                           i.Status != Cartiva.Domain.Enums.InvoiceStatus.Cancelled && 
                           i.DueDate < today)
                .OrderBy(i => i.DueDate)
                .ToList();

            viewModel.PendingInvoiceEntities = allInvoices
                .Where(i => i.Status != Cartiva.Domain.Enums.InvoiceStatus.Paid && 
                           i.Status != Cartiva.Domain.Enums.InvoiceStatus.Cancelled && 
                           i.DueDate >= today)
                .OrderBy(i => i.DueDate)
                .ToList();

            viewModel.PaidInvoiceEntities = allInvoices
                .Where(i => i.Status == Cartiva.Domain.Enums.InvoiceStatus.Paid)
                .OrderByDescending(i => i.PaidDate)
                .ToList();

            // Legacy support: Get deferred orders without Invoice records
            var ordersWithInvoices = allInvoices
                .Where(i => i.OrderHeaderId.HasValue)
                .Select(i => i.OrderHeaderId!.Value)
                .ToHashSet();

            var legacyDeferredOrders = await _db.OrderHeaders
                .Include(o => o.ApplicationUser)
                    .ThenInclude(u => u!.Company)
                .Where(o => o.PaymentStatus == PaymentStatus.Deferred && 
                           !ordersWithInvoices.Contains(o.Id))
                .ToListAsync();

            viewModel.OverdueInvoices = legacyDeferredOrders
                .Where(o => o.PaymentDueDate < today)
                .OrderBy(o => o.PaymentDueDate)
                .ToList();

            viewModel.PendingInvoices = legacyDeferredOrders
                .Where(o => o.PaymentDueDate >= today)
                .OrderBy(o => o.PaymentDueDate)
                .ToList();

            // Paid orders without invoice records
            viewModel.PaidInvoices = await _db.OrderHeaders
                .Include(o => o.ApplicationUser)
                    .ThenInclude(u => u!.Company)
                .Where(o => (o.PaymentStatus == PaymentStatus.Paid || o.PaymentStatus == PaymentStatus.Approved) &&
                           !ordersWithInvoices.Contains(o.Id))
                .OrderByDescending(o => o.PaymentDate)
                .ToListAsync();

            return View(viewModel);
        }

        // GET: Admin/Invoice/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null) return NotFound();

            ViewBag.CartivaContact = _cartivaContact;

            return View(invoice);
        }

        // POST: Admin/Invoice/Send/5 - Mark invoice as sent
        [HttpPost]
        public async Task<IActionResult> Send(int id)
        {
            // Try new Invoice entity first
            var invoice = await _db.Invoices.FindAsync(id);
            if (invoice != null)
            {
                await _invoiceService.SendInvoiceAsync(id);
                TempData["Success"] = $"Invoice {invoice.InvoiceNumber} was sent by email.";
                return RedirectToAction(nameof(Index));
            }

            // Fallback to legacy OrderHeader
            var order = await _db.OrderHeaders.FindAsync(id);
            if (order == null)
                return NotFound();

            var generatedInvoice = await _invoiceService.GetInvoiceByOrderIdAsync(order.Id)
                ?? await _invoiceService.GenerateInvoiceFromOrderAsync(order.Id);

            await _invoiceService.SendInvoiceAsync(generatedInvoice.Id);

            order.InvoiceSent = true;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"Invoice {generatedInvoice.InvoiceNumber} was sent by email.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Invoice/GenerateInvoice/5 - Generate invoice for existing order
        [HttpPost]
        public async Task<IActionResult> GenerateInvoice(int orderId)
        {
            try
            {
                var invoice = await _invoiceService.GenerateInvoiceFromOrderAsync(orderId);
                TempData["Success"] = $"Invoice {invoice.InvoiceNumber} generated successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to generate invoice: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Invoice/RecordPayment - Record payment for invoice
        [HttpPost]
        public async Task<IActionResult> RecordPayment(int invoiceId, decimal amount, string paymentMethod, string? transactionId)
        {
            try
            {
                var method = Enum.TryParse<PaymentMethod>(paymentMethod, out var pm) ? pm : PaymentMethod.BankTransfer;

                await _invoiceService.RecordPaymentAsync(
                    invoiceId, 
                    amount, 
                    method, 
                    transactionId,
                    registeredBy: User.Identity?.Name);

                TempData["Success"] = $"Payment of {amount:C} recorded successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to record payment: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Invoice/PrintInvoice/5
        public async Task<IActionResult> PrintInvoice(int id)
        {
            // Try to find Invoice entity first
            var invoice = await _db.Invoices
                .Include(i => i.OrderHeader)
                    .ThenInclude(o => o!.ApplicationUser)
                        .ThenInclude(u => u!.Company)
                .Include(i => i.Lines)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice != null)
            {
                ViewBag.IsInvoiceEntity = true;
                ViewBag.CartivaContact = _cartivaContact;
                return View("PrintInvoiceNew", invoice);
            }

            // Fallback to OrderHeader (legacy)
            var order = await _db.OrderHeaders
                .Include(o => o.ApplicationUser)
                    .ThenInclude(u => u!.Company)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                        .ThenInclude(pv => pv!.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            var kidNumber = SD.GenerateKIDNumber(order.Id);
            var bankAccount = _configuration["Invoice:BankAccount"] ?? "1234 56 78901";

            ViewBag.KID = kidNumber;
            ViewBag.BankAccount = bankAccount;
            ViewBag.CartivaContact = _cartivaContact;
            ViewBag.IsInvoiceEntity = false;

            return View(order);
        }

        // GET: Admin/Invoice/PrintInvoiceByOrder/5 - Print invoice by OrderId
        public async Task<IActionResult> PrintInvoiceByOrder(int orderId)
        {
            var invoice = await _invoiceService.GetInvoiceByOrderIdAsync(orderId);

            if (invoice != null)
            {
                ViewBag.IsInvoiceEntity = true;
                ViewBag.CartivaContact = _cartivaContact;
                return View("PrintInvoiceNew", invoice);
            }

            // No invoice exists, redirect to legacy print
            return RedirectToAction(nameof(PrintInvoice), new { id = orderId });
        }

        // POST: Admin/Invoice/Cancel/5
        [HttpPost]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            try
            {
                await _invoiceService.CancelInvoiceAsync(id, User.Identity?.Name ?? "Admin", reason);
                TempData["Success"] = "Invoice cancelled successfully.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to cancel invoice: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
