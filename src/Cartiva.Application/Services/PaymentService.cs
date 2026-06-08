using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cartiva.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace Cartiva.Application.Services;

/// <summary>
/// Payment service facade that delegates to the configured payment provider
/// This allows switching providers without changing business logic
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IPaymentProvider _paymentProvider;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentProvider paymentProvider,
        ILogger<PaymentService> logger)
    {
        _paymentProvider = paymentProvider;
        _logger = logger;
    }

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(
        int orderId,
        decimal amount,
        string currency,
        string userId,
        string? description = null)
    {
        var metadata = new Dictionary<string, string>
        {
            ["order_id"] = orderId.ToString(),
            ["user_id"] = userId
        };

        var request = new CreatePaymentIntentRequest(
            Amount: amount,
            Currency: currency,
            Metadata: metadata,
            Description: description ?? $"Order #{orderId}"
        );

        _logger.LogInformation(
            "Creating payment intent for Order {OrderId}, Amount: {Amount} {Currency} via {Provider}",
            orderId, amount, currency, _paymentProvider.ProviderName);

        return await _paymentProvider.CreatePaymentIntentAsync(request);
    }

    public async Task<PaymentConfirmationResult> ConfirmPaymentAsync(string paymentIntentId)
    {
        _logger.LogInformation(
            "Confirming payment {PaymentIntentId} via {Provider}",
            paymentIntentId, _paymentProvider.ProviderName);

        return await _paymentProvider.ConfirmPaymentAsync(paymentIntentId);
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(string paymentIntentId)
    {
        return await _paymentProvider.GetPaymentStatusAsync(paymentIntentId);
    }

    public async Task<RefundResult> RefundPaymentAsync(
        string paymentIntentId,
        decimal? amount = null,
        string? reason = null)
    {
        var request = new RefundPaymentRequest(
            PaymentIntentId: paymentIntentId,
            Amount: amount,
            Reason: reason
        );

        _logger.LogInformation(
            "Refunding payment {PaymentIntentId}, Amount: {Amount} via {Provider}",
            paymentIntentId, amount?.ToString() ?? "FULL", _paymentProvider.ProviderName);

        return await _paymentProvider.RefundPaymentAsync(request);
    }

    public bool ValidateWebhookSignature(string payload, string signature, string secret)
    {
        return _paymentProvider.ValidateWebhookSignature(payload, signature, secret);
    }

    public string GetProviderName()
    {
        return _paymentProvider.ProviderName;
    }
}
