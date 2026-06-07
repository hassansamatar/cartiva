using System.Collections.Generic;
using System.Threading.Tasks;
using Cartiva.Domain;
using Cartiva.Domain.Enums;

namespace Cartiva.Application.Abstractions
{
    public interface IAccountsReceivableAdjustmentService
    {
        /// <summary>
        /// Creates a Stripe Customer for the company using an associated user's email.
        /// </summary>
        Task<string?> ConfigureStripeCustomerAsync(int companyId);

        /// <summary>
        /// Creates an AR adjustment from an approved company return request.
        /// </summary>
        Task<AccountsReceivableAdjustment> CreateFromReturnRequestAsync(
            int returnRequestId,
            int invoiceId,
            int companyId,
            string? createdByUserId = null);

        /// <summary>
        /// Applies the AR adjustment as Stripe credit balance.
        /// </summary>
        Task<bool> ApplyStripeCreditBalanceAsync(int adjustmentId);

        Task<AccountsReceivableAdjustment?> GetByIdAsync(int id);
        Task<List<AccountsReceivableAdjustment>> GetByCompanyIdAsync(int companyId);
        Task<List<AccountsReceivableAdjustment>> GetByInvoiceIdAsync(int invoiceId);
        Task<List<AccountsReceivableAdjustment>> GetAllAsync(
            int? companyId = null,
            ARAdjustmentStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);
        Task<bool> ExistsForReturnRequestAsync(int returnRequestId);
        Task<bool> SendAdjustmentEmailAsync(int adjustmentId);
    }
}