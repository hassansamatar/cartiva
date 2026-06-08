using System.Threading.Tasks;
using Cartiva.Domain.Interfaces;

namespace Cartiva.Application.Services;

/// <summary>
/// Application service for payment operations
/// Acts as facade over payment providers
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Create payment intent for an order
    /// </summary>
    Task<PaymentIntentResult> CreatePaymentIntentAsync(
        int orderId,
        decimal amount,
        string currency,
        string userId,
        string? description = null);

    /// <summary>
    /// Confirm a payment (for server-side confirmation)
    /// </summary>
    Task<PaymentConfirmationResult> ConfirmPaymentAsync(string paymentIntentId);

    /// <summary>
    /// Get current status of a payment
    /// </summary>
    Task<PaymentStatusResult> GetPaymentStatusAsync(string paymentIntentId);

    /// <summary>
    /// Refund a payment
    /// </summary>
    Task<RefundResult> RefundPaymentAsync(
        string paymentIntentId,
        decimal? amount = null,
        string? reason = null);

    /// <summary>
    /// Validate webhook signature
    /// </summary>
    bool ValidateWebhookSignature(string payload, string signature, string secret);

    /// <summary>
    /// Get current payment provider name
    /// </summary>
    string GetProviderName();
}
