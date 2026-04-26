using System.Threading.Tasks;
using Cartiva.Domain;
using Cartiva.Domain.Enums;

namespace Cartiva.Application.Abstractions
{
    /// <summary>
    /// Service for managing Accounts Receivable Adjustments for B2B company returns
    /// </summary>
    public interface IAccountsReceivableAdjustmentService
    {
        /// <summary>
        /// Creates an AR adjustment from an approved company return request
        /// </summary>
        /// <param name="returnRequestId">The approved return request ID</param>
        /// <param name="invoiceId">The invoice ID to adjust</param>
        /// <param name="companyId">The company ID</param>
        /// <param name="createdByUserId">Admin user creating the adjustment</param>
        /// <returns>The created AR adjustment</returns>
        Task<AccountsReceivableAdjustment> CreateFromReturnRequestAsync(
            int returnRequestId,
            int invoiceId,
            int companyId,
            string? createdByUserId = null);

        /// <summary>
        /// Creates a manual AR adjustment (not linked to a return request)
        /// </summary>
        /// <param name="companyId">The company ID</param>
        /// <param name="invoiceId">The invoice ID to adjust</param>
        /// <param name="amount">Adjustment amount (negative reduces balance)</param>
        /// <param name="reason">Reason for the adjustment</param>
        /// <param name="notes">Additional notes</param>
        /// <param name="createdByUserId">Admin user creating the adjustment</param>
        /// <returns>The created AR adjustment</returns>
        Task<AccountsReceivableAdjustment> CreateManualAdjustmentAsync(
            int companyId,
            int invoiceId,
            decimal amount,
            string reason,
            string? notes = null,
            string? createdByUserId = null);

        /// <summary>
        /// Applies the AR adjustment as Stripe credit balance if company has Stripe customer ID
        /// </summary>
        /// <param name="adjustmentId">The adjustment ID to apply</param>
        /// <returns>True if successfully applied, false otherwise</returns>
        Task<bool> ApplyStripeCreditBalanceAsync(int adjustmentId);

        /// <summary>
        /// Gets AR adjustment by ID
        /// </summary>
        Task<AccountsReceivableAdjustment?> GetByIdAsync(int id);

        /// <summary>
        /// Gets all AR adjustments for a company
        /// </summary>
        Task<List<AccountsReceivableAdjustment>> GetByCompanyIdAsync(int companyId);

        /// <summary>
        /// Gets all AR adjustments for an invoice
        /// </summary>
        Task<List<AccountsReceivableAdjustment>> GetByInvoiceIdAsync(int invoiceId);

        /// <summary>
        /// Gets all AR adjustments with optional filtering
        /// </summary>
        Task<List<AccountsReceivableAdjustment>> GetAllAsync(
            int? companyId = null,
            ARAdjustmentStatus? status = null,
            DateTime? fromDate = null,
            DateTime? toDate = null);

        /// <summary>
        /// Checks if an AR adjustment already exists for a return request
        /// </summary>
        Task<bool> ExistsForReturnRequestAsync(int returnRequestId);
    }
}
