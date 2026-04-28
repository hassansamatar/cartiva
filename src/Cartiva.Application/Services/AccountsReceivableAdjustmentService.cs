using Cartiva.Application.Abstractions;
using Cartiva.Domain;
using Cartiva.Domain.Enums;
using Cartiva.Domain.Interfaces;
using Cartiva.Infrastructure.Notifications.Interfaces;
using Cartiva.Infrastructure.Templates.Models;
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

        public async Task<AccountsReceivableAdjustment> CreateFromReturnRequestAsync(
            int returnRequestId,
            int invoiceId,
            int companyId,
            string? createdByUserId = null)
        {
            // Validate inputs
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

            // Check if adjustment already exists for this return
            var existingAdjustment = await _db.Set<AccountsReceivableAdjustment>()
                .FirstOrDefaultAsync(a => a.ReturnRequestId == returnRequestId);

            if (existingAdjustment != null)
            {
                _logger.LogWarning("AR adjustment already exists for return request {ReturnRequestId}", returnRequestId);
                return existingAdjustment;
            }

            // Calculate adjustment amount (negative to reduce receivables)
            var adjustmentAmount = -(returnRequest.RefundAmount ?? 0);
            if (adjustmentAmount == 0)
            {
                // Calculate from order detail if RefundAmount is not set
                if (returnRequest.OrderDetail != null)
                {
                    adjustmentAmount = -(returnRequest.OrderDetail.Price * returnRequest.Quantity);
                }
            }

            // Create AR adjustment
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

            _logger.LogInformation(
                "Created AR adjustment {AdjustmentId} for return {ReturnRequestId}, amount {Amount}. Status: Approved (awaiting manual application).",
                adjustment.Id, returnRequestId, adjustmentAmount);

            return adjustment;
        }

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
            {
                _logger.LogInformation("AR adjustment {AdjustmentId} already applied to Stripe", adjustmentId);
                return true;
            }

            if (string.IsNullOrWhiteSpace(adjustment.Company.StripeCustomerId))
            {
                _logger.LogWarning(
                    "Company {CompanyId} does not have Stripe customer ID, cannot apply credit balance",
                    adjustment.CompanyId);
                return false;
            }

            try
            {
                // Apply credit balance using Stripe Customer Balance Transaction API
                var options = new CustomerBalanceTransactionCreateOptions
                {
                    Amount = (long)(Math.Abs(adjustment.Amount) * 100), // Convert to cents
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

                // Update adjustment
                adjustment.StripeCreditBalanceApplied = true;
                adjustment.StripeCustomerBalanceReference = transaction.Id;
                adjustment.Status = ARAdjustmentStatus.Applied;
                adjustment.AppliedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Applied Stripe credit balance for adjustment {AdjustmentId}, transaction {TransactionId}",
                    adjustmentId, transaction.Id);

                return true;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex,
                    "Stripe error applying credit balance for adjustment {AdjustmentId}: {Error}",
                    adjustmentId, ex.Message);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error applying credit balance for adjustment {AdjustmentId}",
                    adjustmentId);
                return false;
            }
        }

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

        public async Task<AccountsReceivableAdjustment> CreateManualAdjustmentAsync(
            int companyId,
            int invoiceId,
            decimal amount,
            string reason,
            string? notes = null,
            string? createdByUserId = null)
        {
            // Validate inputs
            var company = await _db.Companies.FindAsync(companyId);
            if (company == null)
                throw new InvalidOperationException($"Company {companyId} not found.");

            if (!company.IsActive)
                throw new InvalidOperationException($"Company {company.Name} is not active.");

            var invoice = await _db.Invoices.FindAsync(invoiceId);
            if (invoice == null)
                throw new InvalidOperationException($"Invoice {invoiceId} not found.");

            if (amount == 0)
                throw new InvalidOperationException("Adjustment amount cannot be zero.");

            // Create manual adjustment
            var adjustment = new AccountsReceivableAdjustment
            {
                CompanyId = companyId,
                InvoiceId = invoiceId,
                ReturnRequestId = 0,
                Amount = amount,
                Currency = invoice.Currency,
                Reason = reason,
                Status = ARAdjustmentStatus.Approved,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId,
                Notes = notes
            };

            _db.Set<AccountsReceivableAdjustment>().Add(adjustment);
            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Created manual AR adjustment {AdjustmentId} for company {CompanyId}, amount {Amount}",
                adjustment.Id, companyId, amount);

            // Attempt to apply Stripe credit balance if company has Stripe customer ID
            if (!string.IsNullOrWhiteSpace(company.StripeCustomerId))
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ApplyStripeCreditBalanceAsync(adjustment.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to apply Stripe credit balance for manual adjustment {AdjustmentId}",
                            adjustment.Id);
                    }
                });
            }
            else
            {
                // Mark as applied even without Stripe
                adjustment.Status = ARAdjustmentStatus.Applied;
                adjustment.AppliedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            return adjustment;
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

            return await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Sends an email notification for this AR adjustment using the dedicated template.
        /// </summary>
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
                _logger.LogWarning("AR adjustment {AdjustmentId} not found when trying to send email.", adjustmentId);
                return false;
            }

            // Determine recipient email
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
                var isApplied = adjustment.Status == ARAdjustmentStatus.Applied;

                // Build dictionary for the notification service (matches the email template model properties)
                var templateData = new Dictionary<string, object>
                {
                    ["AdjustmentId"] = adjustment.Id.ToString(),
                    ["CompanyName"] = adjustment.Company.Name,
                    ["Amount"] = adjustment.Amount.ToString("N2", culture),
                    ["Currency"] = adjustment.Currency,
                    ["Reason"] = adjustment.Reason,
                    ["Status"] = adjustment.Status.ToString(),
                    ["CreatedAt"] = adjustment.CreatedAt.ToString("dd MMM yyyy HH:mm", culture),
                    ["AppliedAt"] = adjustment.AppliedAt?.ToString("dd MMM yyyy HH:mm", culture) ?? string.Empty,
                    ["InvoiceNumber"] = adjustment.Invoice?.InvoiceNumber ?? string.Empty,
                    ["Notes"] = adjustment.Notes ?? string.Empty,
                    ["IsApplied"] = isApplied
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
                    Subject: $"Account Receivable Adjustment - {adjustment.Amount:N2} {adjustment.Currency} - {adjustment.Company.Name}"
                ));

                _logger.LogInformation("AR adjustment email for {AdjustmentId} sent to {Recipient}", adjustmentId, recipientEmail);
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