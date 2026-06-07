using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Domain.Enums;
using Cartiva.Domain.Interfaces;
using Cartiva.Infrastructure.Notifications.Interfaces;
using Cartiva.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Stripe;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Cartiva.Application.Services
{
    public class AccountsReceivableAdjustmentService : IAccountsReceivableAdjustmentService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<AccountsReceivableAdjustmentService> _logger;
        private readonly INotificationService _notificationService;

        public AccountsReceivableAdjustmentService(
            ApplicationDbContext db,
            ILogger<AccountsReceivableAdjustmentService> logger,
            INotificationService notificationService)
        {
            _db = db;
            _logger = logger;
            _notificationService = notificationService;
        }

        // =========================
        // STRIPE CUSTOMER CONFIGURATION
        // =========================

        public async Task<string?> ConfigureStripeCustomerAsync(int companyId)
        {
            var company = await _db.Companies.FindAsync(companyId);
            if (company == null)
                throw new InvalidOperationException($"Company {companyId} not found.");

            if (!string.IsNullOrWhiteSpace(company.StripeCustomerId))
                return company.StripeCustomerId;

            // Find an active user associated with this company to get an email
            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.CompanyId == companyId && u.IsActive);

            if (user == null || string.IsNullOrWhiteSpace(user.Email))
                throw new InvalidOperationException($"Company '{company.Name}' has no active user with a valid email address.");

            try
            {
                var customerOptions = new CustomerCreateOptions
                {
                    Name = company.Name,
                    Email = user.Email,
                    Metadata = new Dictionary<string, string>
                    {
                        ["company_id"] = company.Id.ToString(),
                        ["company_name"] = company.Name
                    }
                };

                var customerService = new CustomerService();
                var customer = await customerService.CreateAsync(customerOptions);

                company.StripeCustomerId = customer.Id;
                await _db.SaveChangesAsync();

                _logger.LogInformation("Created Stripe Customer {StripeCustomerId} for company {CompanyId} ({CompanyName})",
                    customer.Id, companyId, company.Name);

                return customer.Id;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error creating customer for company {CompanyId}", companyId);
                throw new InvalidOperationException($"Failed to create Stripe customer: {ex.Message}", ex);
            }
        }

        // =========================
        // AR ADJUSTMENT CREATION (FROM RETURN)
        // =========================

        public async Task<AccountsReceivableAdjustment> CreateFromReturnRequestAsync(
            int returnRequestId,
            int invoiceId,
            int companyId,
            string? createdByUserId = null)
        {
            var returnRequest = await _db.ReturnRequests
                .Include(r => r.OrderDetail)
                .FirstOrDefaultAsync(r => r.Id == returnRequestId);

            if (returnRequest == null)
                throw new InvalidOperationException($"Return request {returnRequestId} not found.");
            if (returnRequest.Status != ReturnStatus.Approved)
                throw new InvalidOperationException($"Return request {returnRequestId} is not approved.");

            var invoice = await _db.Invoices.FindAsync(invoiceId);
            if (invoice == null)
                throw new InvalidOperationException($"Invoice {invoiceId} not found.");

            var company = await _db.Companies.FindAsync(companyId);
            if (company == null)
                throw new InvalidOperationException($"Company {companyId} not found.");

            var existingAdjustment = await _db.Set<AccountsReceivableAdjustment>()
                .FirstOrDefaultAsync(a => a.ReturnRequestId == returnRequestId);
            if (existingAdjustment != null)
            {
                _logger.LogWarning("AR adjustment already exists for return request {ReturnRequestId}", returnRequestId);
                return existingAdjustment;
            }

            var adjustmentAmount = -(returnRequest.RefundAmount ?? 0);
            if (adjustmentAmount == 0 && returnRequest.OrderDetail != null)
                adjustmentAmount = -(returnRequest.OrderDetail.Price * returnRequest.Quantity);

            var adjustment = new AccountsReceivableAdjustment
            {
                CompanyId = companyId,
                InvoiceId = invoiceId,
                ReturnRequestId = returnRequestId,
                Amount = adjustmentAmount,
                Currency = invoice.Currency,
                Reason = $"Return approved for Order #{returnRequest.OrderDetail?.OrderHeaderId ?? 0} - {returnRequest.Reason}",
                Status = ARAdjustmentStatus.Approved,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId,
                Notes = returnRequest.Description
            };

            _db.Set<AccountsReceivableAdjustment>().Add(adjustment);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Created AR adjustment {AdjustmentId} for return {ReturnRequestId}, amount {Amount}",
                adjustment.Id, returnRequestId, adjustmentAmount);

            return adjustment;
        }

        // =========================
        // STRIPE CREDIT BALANCE APPLICATION
        // =========================

        public async Task<bool> ApplyStripeCreditBalanceAsync(int adjustmentId)
        {
            var adjustment = await _db.Set<AccountsReceivableAdjustment>()
                .Include(a => a.Company)
                .Include(a => a.Invoice)
                .FirstOrDefaultAsync(a => a.Id == adjustmentId);

            if (adjustment == null)
            {
                _logger.LogWarning("AR adjustment {AdjustmentId} not found", adjustmentId);
                return false;
            }

            if (adjustment.StripeCreditBalanceApplied)
                return true;

            if (string.IsNullOrWhiteSpace(adjustment.Company.StripeCustomerId))
            {
                _logger.LogWarning("Company {CompanyId} has no Stripe customer ID", adjustment.CompanyId);
                return false;
            }

            try
            {
                var options = new CustomerBalanceTransactionCreateOptions
                {
                    Amount = (long)(Math.Abs(adjustment.Amount) * 100),
                    Currency = adjustment.Currency.ToLowerInvariant(),
                    Description = adjustment.Reason,
                    Metadata = new Dictionary<string, string>
                    {
                        ["adjustment_id"] = adjustment.Id.ToString(),
                        ["invoice_id"] = adjustment.InvoiceId.ToString(),
                        ["return_request_id"] = adjustment.ReturnRequestId.ToString(),
                        ["company_id"] = adjustment.CompanyId.ToString()
                    }
                };

                var service = new CustomerBalanceTransactionService();
                var transaction = await service.CreateAsync(adjustment.Company.StripeCustomerId, options);

                adjustment.StripeCreditBalanceApplied = true;
                adjustment.StripeCustomerBalanceReference = transaction.Id;
                adjustment.Status = ARAdjustmentStatus.Applied;
                adjustment.AppliedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                _logger.LogInformation("Applied Stripe credit balance for adjustment {AdjustmentId}, transaction {TransactionId}",
                    adjustmentId, transaction.Id);

                // Send email notification after successful application
                await SendAdjustmentEmailAsync(adjustmentId);

                return true;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error applying credit balance for adjustment {AdjustmentId}", adjustmentId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error applying credit balance for adjustment {AdjustmentId}", adjustmentId);
                return false;
            }
        }

        // =========================
        // QUERIES
        // =========================

        public async Task<AccountsReceivableAdjustment?> GetByIdAsync(int id)
        {
            return await _db.Set<AccountsReceivableAdjustment>()
                .Include(a => a.Company)
                .Include(a => a.Invoice)
                .Include(a => a.ReturnRequest)
                    .ThenInclude(r => r.OrderDetail)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<List<AccountsReceivableAdjustment>> GetByCompanyIdAsync(int companyId)
        {
            return await _db.Set<AccountsReceivableAdjustment>()
                .Include(a => a.Invoice)
                .Include(a => a.ReturnRequest)
                .Where(a => a.CompanyId == companyId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<AccountsReceivableAdjustment>> GetByInvoiceIdAsync(int invoiceId)
        {
            return await _db.Set<AccountsReceivableAdjustment>()
                .Include(a => a.Company)
                .Include(a => a.ReturnRequest)
                .Where(a => a.InvoiceId == invoiceId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ExistsForReturnRequestAsync(int returnRequestId)
        {
            return await _db.Set<AccountsReceivableAdjustment>()
                .AnyAsync(a => a.ReturnRequestId == returnRequestId);
        }

        public async Task<List<AccountsReceivableAdjustment>> GetAllAsync(
            int? companyId = null,
            ARAdjustmentStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null)
        {
            var query = _db.Set<AccountsReceivableAdjustment>()
                .Include(a => a.Company)
                .Include(a => a.Invoice)
                .Include(a => a.ReturnRequest)
                .AsQueryable();

            if (companyId.HasValue)
                query = query.Where(a => a.CompanyId == companyId.Value);
            if (status.HasValue)
                query = query.Where(a => a.Status == status.Value);
            if (fromDate.HasValue)
                query = query.Where(a => a.CreatedAt >= fromDate.Value);
            if (toDate.HasValue)
                query = query.Where(a => a.CreatedAt <= toDate.Value);

            return await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
        }

        // =========================
        // EMAIL NOTIFICATION
        // =========================

        public async Task<bool> SendAdjustmentEmailAsync(int adjustmentId)
        {
            var adjustment = await _db.AccountsReceivableAdjustments
                .Include(a => a.Company)
                .Include(a => a.Invoice)
                    .ThenInclude(i => i!.OrderHeader)
                        .ThenInclude(o => o!.ApplicationUser)
                .Include(a => a.ReturnRequest)
                    .ThenInclude(rr => rr!.OrderDetail)
                        .ThenInclude(od => od.OrderHeader)
                            .ThenInclude(oh => oh.ApplicationUser)
                .FirstOrDefaultAsync(a => a.Id == adjustmentId);

            if (adjustment == null)
            {
                _logger.LogWarning("AR adjustment {AdjustmentId} not found", adjustmentId);
                return false;
            }

            var recipientEmail = adjustment.Invoice?.OrderHeader?.ApplicationUser?.Email
                ?? adjustment.Invoice?.CustomerEmail
                ?? adjustment.ReturnRequest?.OrderDetail?.OrderHeader?.ApplicationUser?.Email;

            if (string.IsNullOrWhiteSpace(recipientEmail))
            {
                _logger.LogWarning("No customer email found for AR adjustment {AdjustmentId}", adjustmentId);
                return false;
            }

            try
            {
                var culture = CultureInfo.GetCultureInfo("nb-NO");
                var templateData = new Dictionary<string, object>
                {
                    ["AdjustmentId"] = adjustment.Id.ToString(),
                    ["CompanyName"] = adjustment.Company.Name ?? "",
                    ["Amount"] = adjustment.Amount.ToString("N2", culture),
                    ["Currency"] = adjustment.Currency,
                    ["Reason"] = adjustment.Reason,
                    ["Status"] = adjustment.Status.ToString(),
                    ["CreatedAt"] = adjustment.CreatedAt.ToString("dd MMM yyyy HH:mm", culture),
                    ["AppliedAt"] = adjustment.AppliedAt?.ToString("dd MMM yyyy HH:mm", culture) ?? string.Empty,
                    ["InvoiceNumber"] = adjustment.Invoice?.InvoiceNumber ?? string.Empty,
                    ["Notes"] = adjustment.Notes ?? string.Empty
                };

                var userId = adjustment.Invoice?.OrderHeader?.ApplicationUserId
                    ?? adjustment.ReturnRequest?.OrderDetail?.OrderHeader?.ApplicationUserId;

                await _notificationService.SendAsync(new NotificationRequest(
                    Recipient: recipientEmail,
                    Type: NotificationType.ARAdjustmentApplied,
                    TemplateData: templateData,
                    UserId: userId,
                    ReferenceId: adjustment.Id.ToString(),
                    ReferenceType: "ARAdjustment",
                    Subject: $"Account Receivable Adjustment - {Math.Abs(adjustment.Amount):N2} {adjustment.Currency} - {adjustment.Company.Name}"
                ));

                _logger.LogInformation("AR adjustment email sent for {AdjustmentId} to {Recipient}", adjustmentId, recipientEmail);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send AR adjustment email for ID {AdjustmentId}", adjustmentId);
                return false;
            }
        }
    }
}