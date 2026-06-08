using System.Collections.Generic;

namespace Cartiva.Domain.Interfaces;

/// <summary>
/// Request to create a payment intent
/// </summary>
public record CreatePaymentIntentRequest(
    decimal Amount,
    string Currency,
    Dictionary<string, string> Metadata,
    string? CustomerId = null,
    string? Description = null,
    bool CaptureMethod = true
);

/// <summary>
/// Result from creating a payment intent
/// </summary>
public record PaymentIntentResult(
    bool Success,
    string? PaymentIntentId,
    string? ClientSecret,
    string? ErrorMessage = null,
    PaymentIntentStatus Status = PaymentIntentStatus.RequiresPaymentMethod
);

/// <summary>
/// Result from confirming a payment
/// </summary>
public record PaymentConfirmationResult(
    bool Success,
    string PaymentIntentId,
    PaymentIntentStatus Status,
    string? ErrorMessage = null,
    decimal? AmountReceived = null
);

/// <summary>
/// Result from checking payment status
/// </summary>
public record PaymentStatusResult(
    string PaymentIntentId,
    PaymentIntentStatus Status,
    decimal Amount,
    string Currency,
    Dictionary<string, string> Metadata
);

/// <summary>
/// Request to refund a payment
/// </summary>
public record RefundPaymentRequest(
    string PaymentIntentId,
    decimal? Amount = null,
    string? Reason = null,
    Dictionary<string, string>? Metadata = null
);

/// <summary>
/// Result from refund operation
/// </summary>
public record RefundResult(
    bool Success,
    string? RefundId,
    decimal? RefundedAmount,
    string? ErrorMessage = null,
    RefundStatus Status = RefundStatus.Pending
);

/// <summary>
/// Payment intent status (provider-agnostic)
/// </summary>
public enum PaymentIntentStatus
{
    RequiresPaymentMethod,
    RequiresConfirmation,
    RequiresAction,
    Processing,
    Succeeded,
    Canceled,
    Failed
}

/// <summary>
/// Refund status (provider-agnostic)
/// </summary>
public enum RefundStatus
{
    Pending,
    Succeeded,
    Failed,
    Canceled
}
