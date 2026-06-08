using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cartiva.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace Cartiva.Infrastructure.PaymentService;

/// <summary>
/// Stripe implementation of IPaymentProvider
/// Wraps Stripe SDK calls with our payment abstraction
/// </summary>
public class StripePaymentProvider : IPaymentProvider
{
    private readonly StripeSettings _settings;
    private readonly ILogger<StripePaymentProvider> _logger;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly RefundService _refundService;

    public string ProviderName => "Stripe";

    public StripePaymentProvider(
        IOptions<StripeSettings> settings,
        ILogger<StripePaymentProvider> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // Configure Stripe API key
        StripeConfiguration.ApiKey = _settings.SecretKey;

        _paymentIntentService = new PaymentIntentService();
        _refundService = new RefundService();
    }

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(CreatePaymentIntentRequest request)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100), // Convert to cents/øre
                Currency = request.Currency.ToLower(),
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                },
                Metadata = request.Metadata,
                Description = request.Description,
                Customer = request.CustomerId
            };

            if (!request.CaptureMethod)
            {
                options.CaptureMethod = "manual";
            }

            var paymentIntent = await _paymentIntentService.CreateAsync(options);

            _logger.LogInformation(
                "[{Provider}] Created PaymentIntent {PaymentIntentId} for {Amount} {Currency}",
                ProviderName, paymentIntent.Id, request.Amount, request.Currency);

            return new PaymentIntentResult(
                Success: true,
                PaymentIntentId: paymentIntent.Id,
                ClientSecret: paymentIntent.ClientSecret,
                Status: MapStripeStatus(paymentIntent.Status)
            );
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[{Provider}] Failed to create PaymentIntent", ProviderName);
            return new PaymentIntentResult(
                Success: false,
                PaymentIntentId: null,
                ClientSecret: null,
                ErrorMessage: ex.Message
            );
        }
    }

    public async Task<PaymentConfirmationResult> ConfirmPaymentAsync(string paymentIntentId)
    {
        try
        {
            var options = new PaymentIntentConfirmOptions();
            var paymentIntent = await _paymentIntentService.ConfirmAsync(paymentIntentId, options);

            _logger.LogInformation(
                "[{Provider}] Confirmed PaymentIntent {PaymentIntentId}, Status: {Status}",
                ProviderName, paymentIntentId, paymentIntent.Status);

            return new PaymentConfirmationResult(
                Success: paymentIntent.Status == "succeeded",
                PaymentIntentId: paymentIntent.Id,
                Status: MapStripeStatus(paymentIntent.Status),
                AmountReceived: paymentIntent.AmountReceived / 100m
            );
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[{Provider}] Failed to confirm PaymentIntent {PaymentIntentId}",
                ProviderName, paymentIntentId);

            return new PaymentConfirmationResult(
                Success: false,
                PaymentIntentId: paymentIntentId,
                Status: PaymentIntentStatus.Failed,
                ErrorMessage: ex.Message
            );
        }
    }

    public async Task<PaymentStatusResult> GetPaymentStatusAsync(string paymentIntentId)
    {
        try
        {
            var paymentIntent = await _paymentIntentService.GetAsync(paymentIntentId);

            return new PaymentStatusResult(
                PaymentIntentId: paymentIntent.Id,
                Status: MapStripeStatus(paymentIntent.Status),
                Amount: paymentIntent.Amount / 100m,
                Currency: paymentIntent.Currency.ToUpper(),
                Metadata: paymentIntent.Metadata ?? new Dictionary<string, string>()
            );
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[{Provider}] Failed to get status for PaymentIntent {PaymentIntentId}",
                ProviderName, paymentIntentId);
            throw;
        }
    }

    public async Task<RefundResult> RefundPaymentAsync(RefundPaymentRequest request)
    {
        try
        {
            var options = new RefundCreateOptions
            {
                PaymentIntent = request.PaymentIntentId,
                Reason = request.Reason switch
                {
                    "duplicate" => "duplicate",
                    "fraudulent" => "fraudulent",
                    _ => "requested_by_customer"
                },
                Metadata = request.Metadata
            };

            // If partial refund, specify amount
            if (request.Amount.HasValue)
            {
                options.Amount = (long)(request.Amount.Value * 100);
            }

            var refund = await _refundService.CreateAsync(options);

            _logger.LogInformation(
                "[{Provider}] Created refund {RefundId} for PaymentIntent {PaymentIntentId}, Amount: {Amount}",
                ProviderName, refund.Id, request.PaymentIntentId, refund.Amount / 100m);

            return new RefundResult(
                Success: true,
                RefundId: refund.Id,
                RefundedAmount: refund.Amount / 100m,
                Status: MapRefundStatus(refund.Status)
            );
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "[{Provider}] Failed to refund PaymentIntent {PaymentIntentId}",
                ProviderName, request.PaymentIntentId);

            return new RefundResult(
                Success: false,
                RefundId: null,
                RefundedAmount: null,
                ErrorMessage: ex.Message,
                Status: RefundStatus.Failed
            );
        }
    }

    public bool ValidateWebhookSignature(string payload, string signature, string secret)
    {
        try
        {
            // Stripe's signature validation
            EventUtility.ConstructEvent(payload, signature, secret, throwOnApiVersionMismatch: false);
            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "[{Provider}] Webhook signature validation failed", ProviderName);
            return false;
        }
    }

    // Helper: Map Stripe status to our abstraction
    private PaymentIntentStatus MapStripeStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "requires_payment_method" => PaymentIntentStatus.RequiresPaymentMethod,
            "requires_confirmation" => PaymentIntentStatus.RequiresConfirmation,
            "requires_action" => PaymentIntentStatus.RequiresAction,
            "processing" => PaymentIntentStatus.Processing,
            "succeeded" => PaymentIntentStatus.Succeeded,
            "canceled" => PaymentIntentStatus.Canceled,
            _ => PaymentIntentStatus.Failed
        };
    }

    // Helper: Map Stripe refund status to our abstraction
    private RefundStatus MapRefundStatus(string stripeStatus)
    {
        return stripeStatus switch
        {
            "pending" => RefundStatus.Pending,
            "succeeded" => RefundStatus.Succeeded,
            "failed" => RefundStatus.Failed,
            "canceled" => RefundStatus.Canceled,
            _ => RefundStatus.Failed
        };
    }
}
