using Cartiva.Domain;
using Cartiva.Domain.ViewModels;
using Cartiva.Persistence;
using Cartiva.Shared.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Cryptography;
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

        public InvoiceController(ApplicationDbContext db, CartivaContact cartivaContact, IConfiguration configuration)
        {
            _db = db;
            _cartivaContact = cartivaContact;
            _configuration = configuration;
        }

        // GET: Admin/Invoice/Index
        public async Task<IActionResult> Index()
        {
            var today = DateOnly.FromDateTime(DateTime.Now);

            // Deferred orders (not yet paid) – split into Overdue and Pending
            var deferredOrders = await _db.OrderHeaders
                .Include(o => o.ApplicationUser)
                .ThenInclude(u => u.Company)
                .Where(o => o.PaymentStatus == "Deferred")
                .ToListAsync();

            var overdueOrders = deferredOrders
                .Where(o => o.PaymentDueDate < today && !o.InvoiceSent)
                .OrderBy(o => o.PaymentDueDate)
                .ToList();

            var pendingOrders = deferredOrders
                .Where(o => o.PaymentDueDate >= today && !o.InvoiceSent)
                .OrderBy(o => o.PaymentDueDate)
                .ToList();

            // Paid orders (example – adjust status value as needed)
            var paidOrders = await _db.OrderHeaders
                .Include(o => o.ApplicationUser)
                .ThenInclude(u => u.Company)
                .Where(o => o.PaymentStatus == "Paid" || o.PaymentStatus == "Approved")
                .OrderByDescending(o => o.PaymentDate)
                .ToListAsync();

            var viewModel = new InvoiceDashboardViewModel
            {
                OverdueInvoices = overdueOrders,
                PendingInvoices = pendingOrders,
                PaidInvoices = paidOrders
            };

            return View(viewModel);
        }

        // GET: Admin/Invoice/Overdue (kept for backward compatibility)
        public async Task<IActionResult> Overdue()
        {
            var overdueOrders = await _db.OrderHeaders
                .Include(o => o.ApplicationUser)
                .Where(o => o.PaymentStatus == "Deferred" &&
                            o.PaymentDueDate < DateOnly.FromDateTime(DateTime.Now) &&
                            !o.InvoiceSent &&
                            o.ApplicationUser.CompanyId != null)
                .ToListAsync();
            return View(overdueOrders);
        }

        // POST: Admin/Invoice/Send/5
        [HttpPost]
        public async Task<IActionResult> Send(int id)
        {
            var order = await _db.OrderHeaders.FindAsync(id);
            if (order == null || order.InvoiceSent)
                return NotFound();

            order.InvoiceSent = true;
            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Overdue));
        }
        // KID number generation logic (example, adjust as needed)
        private string GenerateKIDNumber(int orderId)
        {
            // Pad orderId to 15 digits (leaving one digit for checksum)
            string baseNumber = orderId.ToString().PadLeft(15, '0');
            // Calculate Mod10 checksum (Luhn algorithm) – simplified version
            int sum = 0;
            bool alternate = true;
            for (int i = baseNumber.Length - 1; i >= 0; i--)
            {
                int digit = int.Parse(baseNumber[i].ToString());
                if (alternate)
                {
                    digit *= 2;
                    if (digit > 9) digit -= 9;
                }
                sum += digit;
                alternate = !alternate;
            }
            int checksum = (sum * 9) % 10;
            return baseNumber + checksum.ToString();
        }
        // GET: Admin/Invoice/PrintInvoice/5
        public async Task<IActionResult> PrintInvoice(int id)
        {
            var order = await _db.OrderHeaders
                .Include(o => o.ApplicationUser)
                    .ThenInclude(u => u.Company)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.ProductVariant)
                    .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Get bank account and company info from configuration
            
            var kidNumber = GenerateKIDNumber(order.Id);

            var bankAccount = _configuration["Invoice:BankAccount"] ?? "1234 56 78901";
            ViewBag.KID = kidNumber;
            ViewBag.BankAccount = bankAccount;
            ViewBag.CartivaContact = _cartivaContact;

            return View(order);
        }
    }
}
