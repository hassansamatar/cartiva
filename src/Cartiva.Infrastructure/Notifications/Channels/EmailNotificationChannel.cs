using System.Text.Json;
using Cartiva.Domain;
using Cartiva.Domain.Enums;
using Cartiva.Infrastructure.Notifications.Interfaces;
using Cartiva.Infrastructure.Notifications.Templates.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Cartiva.Infrastructure.Notifications.Channels;

public class EmailNotificationChannel : INotificationChannel
{
    private readonly ISmtpEmailSender _smtpSender;
    private readonly ITemplateRenderer _templateRenderer;
    private readonly ILogger<EmailNotificationChannel> _logger;
    private readonly AsyncRetryPolicy _retryPolicy;

    public EmailNotificationChannel(
        ISmtpEmailSender smtpSender,
        ITemplateRenderer templateRenderer,
        ILogger<EmailNotificationChannel> logger)
    {
        _smtpSender = smtpSender;
        _templateRenderer = templateRenderer;
        _logger = logger;

        // Configure Polly retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        exception,
                        "Email send attempt {RetryCount} failed. Waiting {TimeSpan} before next retry.",
                        retryCount,
                        timeSpan);
                });
    }

    public async Task<bool> SendAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            // Render email content using template
            var htmlBody = await RenderTemplateAsync(notification, cancellationToken);

            // Use subject from notification or generate default
            var subject = notification.Subject ?? GenerateDefaultSubject(notification.Type);

            // Send with retry policy
            return await _retryPolicy.ExecuteAsync(async () =>
            {
                return await _smtpSender.SendEmailAsync(
                    notification.Recipient,
                    subject,
                    htmlBody,
                    cancellationToken);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send email notification {NotificationId} to {Recipient} after retries",
                notification.Id,
                notification.Recipient);
            return false;
        }
    }

    private async Task<string> RenderTemplateAsync(Notification notification, CancellationToken cancellationToken)
    {
        var templateData = notification.TemplateData != null
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(notification.TemplateData)
            : new Dictionary<string, object>();

        var templateName = GetTemplateName(notification.Type);

        // Create strongly-typed model based on notification type
        return notification.Type switch
        {
            NotificationType.OrderConfirmation => await _templateRenderer.RenderAsync(
                templateName,
                CreateOrderConfirmationModel(templateData),
                cancellationToken),

            NotificationType.OrderShipped => await _templateRenderer.RenderAsync(
                templateName,
                CreateOrderShippedModel(templateData),
                cancellationToken),

            NotificationType.PasswordReset => await _templateRenderer.RenderAsync(
                templateName,
                CreatePasswordResetModel(templateData),
                cancellationToken),

            NotificationType.WelcomeEmail => await _templateRenderer.RenderAsync(
                templateName,
                CreateWelcomeEmailModel(templateData),
                cancellationToken),

            _ => await _templateRenderer.RenderAsync(
                "Generic",
                new GenericEmailModel
                {
                    Subject = notification.Subject ?? "Notification",
                    Body = templateData.TryGetValue("body", out var body) ? body.ToString() ?? "" : "",
                    Data = templateData
                },
                cancellationToken)
        };
    }

    private string GetTemplateName(NotificationType type) => type switch
    {
        NotificationType.OrderConfirmation => "OrderConfirmation",
        NotificationType.OrderShipped => "OrderShipped",
        NotificationType.OrderDelivered => "OrderDelivered",
        NotificationType.OrderCancelled => "OrderCancelled",
        NotificationType.PaymentReceived => "PaymentReceived",
        NotificationType.PaymentFailed => "PaymentFailed",
        NotificationType.PasswordReset => "PasswordReset",
        NotificationType.EmailVerification => "EmailVerification",
        NotificationType.WelcomeEmail => "WelcomeEmail",
        NotificationType.InvoiceGenerated => "InvoiceGenerated",
        NotificationType.ReturnRequestReceived => "ReturnRequestReceived",
        NotificationType.ReturnRequestApproved => "ReturnRequestApproved",
        NotificationType.ReturnRequestRejected => "ReturnRequestRejected",
        _ => "Generic"
    };

    private string GenerateDefaultSubject(NotificationType type) => type switch
    {
        NotificationType.OrderConfirmation => "Order Confirmation",
        NotificationType.OrderShipped => "Your Order Has Been Shipped",
        NotificationType.OrderDelivered => "Your Order Has Been Delivered",
        NotificationType.OrderCancelled => "Order Cancelled",
        NotificationType.PaymentReceived => "Payment Received",
        NotificationType.PaymentFailed => "Payment Failed",
        NotificationType.PasswordReset => "Password Reset Request",
        NotificationType.EmailVerification => "Verify Your Email",
        NotificationType.WelcomeEmail => "Welcome to Cartiva",
        NotificationType.InvoiceGenerated => "Your Invoice is Ready",
        NotificationType.ReturnRequestReceived => "Return Request Received",
        NotificationType.ReturnRequestApproved => "Return Request Approved",
        NotificationType.ReturnRequestRejected => "Return Request Rejected",
        _ => "Notification"
    };

    private OrderConfirmationModel CreateOrderConfirmationModel(Dictionary<string, object> data)
    {
        return new OrderConfirmationModel
        {
            OrderId = data.TryGetValue("orderId", out var orderId)
                ? orderId.ToString() ?? ""
                : data.TryGetValue("orderNumber", out var orderNum)
                    ? orderNum.ToString() ?? ""
                    : "",
            Name = data.TryGetValue("name", out var name)
                ? name.ToString() ?? ""
                : data.TryGetValue("customerName", out var customerName)
                    ? customerName.ToString() ?? ""
                    : "",
            OrderDate = data.TryGetValue("orderDate", out var date) ? date.ToString() ?? "" : "",
            TotalAmount = data.TryGetValue("totalAmount", out var total) ? total.ToString() ?? "" : "",
            Items = new List<string>()
        };
    }

    private OrderShippedModel CreateOrderShippedModel(Dictionary<string, object> data)
    {
        return new OrderShippedModel
        {
            OrderId = data.TryGetValue("orderId", out var orderId)
                ? orderId.ToString() ?? ""
                : data.TryGetValue("orderNumber", out var orderNum)
                    ? orderNum.ToString() ?? ""
                    : "",
            TrackingNumber = data.TryGetValue("trackingNumber", out var tracking) ? tracking.ToString() ?? "" : "",
            Carrier = data.TryGetValue("carrier", out var carrier) ? carrier.ToString() ?? "" : "",
            EstimatedDeliveryDate = data.TryGetValue("estimatedDeliveryDate", out var date) ? date.ToString() ?? "" : ""
        };
    }

    private PasswordResetModel CreatePasswordResetModel(Dictionary<string, object> data)
    {
        return new PasswordResetModel
        {
            Name = data.TryGetValue("name", out var name)
                ? name.ToString() ?? ""
                : data.TryGetValue("userName", out var userName)
                    ? userName.ToString() ?? ""
                    : "",
            ResetLink = data.TryGetValue("resetLink", out var link) ? link.ToString() ?? "" : "",
            ExpirationTime = data.TryGetValue("expirationTime", out var exp) ? exp.ToString() ?? "24 hours" : "24 hours"
        };
    }

    private WelcomeEmailModel CreateWelcomeEmailModel(Dictionary<string, object> data)
    {
        return new WelcomeEmailModel
        {
            Name = data.TryGetValue("name", out var name)
                ? name.ToString() ?? ""
                : data.TryGetValue("userName", out var userName)
                    ? userName.ToString() ?? ""
                    : "",
            VerificationLink = data.TryGetValue("verificationLink", out var link) ? link.ToString() : null
        };
    }
}
