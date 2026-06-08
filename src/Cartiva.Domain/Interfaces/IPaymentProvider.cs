using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cartiva.Domain.Interfaces;

/// <summary>
/// Abstraction for payment providers (Stripe, PayPal, Vipps, etc.)
/// Allows pluggable payment implementations without coupling to specific provider
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Provider name (e.g., "Stripe", "PayPal", "Vipps")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Create a payment intent for the given amount
    /// </summary>
    Task<PaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentIntentRequest request);

    /// <summary>
    /// Confirm a payment (for server-side confirmation scenarios)
    /// </summary>
    Task<PaymentConfirmationResult> ConfirmPaymentAsync(string paymentIntentId);

    /// <summary>
    /// Get current status of a payment
    /// </summary>
    Task<PaymentStatusResult> GetPaymentStatusAsync(string paymentIntentId);

    /// <summary>
    /// Refund a payment (full or partial)
    /// </summary>
    Task<RefundResult> RefundPaymentAsync(RefundPaymentRequest request);

    /// <summary>
    /// Validate webhook signature for security
    /// </summary>
    bool ValidateWebhookSignature(string payload, string signature, string secret);
}
