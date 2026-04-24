using System.Text.Json;
using Cartiva.Domain;
using Cartiva.Domain.Enums;
using Cartiva.Infrastructure.Notifications.Interfaces;
using Cartiva.Infrastructure.Templates.Models;
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
                CreateTemplateOrderConfirmationModel(templateData),
                cancellationToken),

            NotificationType.OrderShipped => await _templateRenderer.RenderAsync(
                templateName,
                CreateTemplateOrderShippedModel(templateData),
                cancellationToken),

            NotificationType.OrderDelivered => await _templateRenderer.RenderAsync(
                templateName,
                CreateTemplateOrderDeliveredModel(templateData),
                cancellationToken),

            NotificationType.OrderCancelled => await _templateRenderer.RenderAsync(
                templateName,
                CreateTemplateOrderCancelledModel(templateData),
                cancellationToken),

            NotificationType.PaymentReceived => await _templateRenderer.RenderAsync(
                templateName,
                CreatePaymentReceivedModel(templateData),
                cancellationToken),

            NotificationType.InvoiceGenerated => await _templateRenderer.RenderAsync(
                templateName,
                CreateInvoiceGeneratedModel(templateData),
                cancellationToken),

            NotificationType.ReturnRequestReceived => await _templateRenderer.RenderAsync(
                templateName,
                CreateReturnRequestReceivedModel(templateData),
                cancellationToken),

            NotificationType.ReturnRequestApproved => await _templateRenderer.RenderAsync(
                templateName,
                CreateReturnRequestApprovedModel(templateData),
                cancellationToken),

            NotificationType.ReturnRequestRejected => await _templateRenderer.RenderAsync(
                templateName,
                CreateReturnRequestRejectedModel(templateData),
                cancellationToken),

            NotificationType.PasswordReset => await _templateRenderer.RenderAsync(
                templateName,
                CreatePasswordResetTemplateModel(templateData),
                cancellationToken),

            NotificationType.WelcomeEmail => await _templateRenderer.RenderAsync(
                templateName,
                CreateWelcomeEmailTemplateModel(templateData),
                cancellationToken),

            _ => await _templateRenderer.RenderAsync(
                "Generic",
                CreateGenericNotificationModel(notification, templateData),
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

    private string BuildOrderConfirmationBody(Dictionary<string, object> data)
    {
        var model = CreateOrderConfirmationModel(data);
        var name = string.IsNullOrWhiteSpace(model.Name) ? "Customer" : model.Name;
        var orderId = string.IsNullOrWhiteSpace(model.OrderId) ? "N/A" : model.OrderId;
        var orderDate = string.IsNullOrWhiteSpace(model.OrderDate) ? "N/A" : model.OrderDate;
        var totalAmount = string.IsNullOrWhiteSpace(model.TotalAmount) ? "N/A" : model.TotalAmount;

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <meta name='viewport' content='width=device-width, initial-scale=1.0' />
    <title>Order Confirmation</title>
</head>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;'>
    <h1>Order Confirmation</h1>
    <p>Hello {name},</p>
    <p>Thank you for your order.</p>
    <p><strong>Order Number:</strong> {orderId}</p>
    <p><strong>Order Date:</strong> {orderDate}</p>
    <p><strong>Total Amount:</strong> {totalAmount}</p>
    <p>We appreciate your business.</p>
    <p>Best regards,<br />The Cartiva Team</p>
</body>
</html>";
    }

    private OrderConfirmationModel CreateTemplateOrderConfirmationModel(Dictionary<string, object> data)
    {
        var model = CreateOrderConfirmationModel(data);

        return new OrderConfirmationModel
        {
            OrderId = model.OrderId,
            Name = model.Name,
            OrderDate = model.OrderDate,
            TotalAmount = model.TotalAmount,
            Items = model.Items
        };
    }

    private OrderShippedModel CreateTemplateOrderShippedModel(Dictionary<string, object> data)
    {
        return new OrderShippedModel
        {
            Id = ParseInt(data, "shipmentId"),
            OrderHeaderId = ParseInt(data, "orderId", "orderNumber"),
            TrackingNumber = data.TryGetValue("trackingNumber", out var tracking) ? tracking.ToString() ?? "" : "",
            Carrier = data.TryGetValue("carrier", out var carrier) ? carrier.ToString() ?? "" : "",
            Service = data.TryGetValue("service", out var service) ? service.ToString() : null,
            TrackingUrl = data.TryGetValue("trackingUrl", out var trackingUrl) ? trackingUrl.ToString() : null,
            ShippingDate = ParseDateTime(data, "shippingDate"),
            ShippedDate = ParseDateTime(data, "shippedDate"),
            ShipmentStatus = data.TryGetValue("shipmentStatus", out var shipmentStatus) ? shipmentStatus.ToString() ?? string.Empty : string.Empty,
            DeliveredDate = ParseDateTime(data, "estimatedDeliveryDate")
        };
    }

    private OrderDeliveredModel CreateTemplateOrderDeliveredModel(Dictionary<string, object> data)
    {
        return new OrderDeliveredModel
        {
            Id = ParseInt(data, "shipmentId"),
            OrderHeaderId = ParseInt(data, "orderId", "orderNumber"),
            CustomerName = GetString(data, "customerName", "name"),
            TrackingNumber = GetNullableString(data, "trackingNumber"),
            Carrier = GetNullableString(data, "carrier"),
            Service = GetNullableString(data, "service"),
            TrackingUrl = GetNullableString(data, "trackingUrl"),
            ShippingDate = ParseDateTime(data, "shippingDate"),
            ShippedDate = ParseDateTime(data, "shippedDate"),
            DeliveredDate = ParseDateTime(data, "deliveredDate", "deliveryDate"),
            ShipmentStatus = GetString(data, "shipmentStatus")
        };
    }

    private OrderCancelledModel CreateTemplateOrderCancelledModel(Dictionary<string, object> data)
    {
        return new OrderCancelledModel
        {
            Id = ParseInt(data, "orderId", "orderNumber"),
            Name = GetString(data, "name", "customerName"),
            OrderDate = ParseDateTime(data, "orderDate") ?? DateTime.UtcNow,
            OrderTotal = ParseDecimal(data, "orderTotal", "totalAmount"),
            Currency = GetString(data, "currency", defaultValue: "NOK"),
            OrderStatus = GetNullableString(data, "orderStatus"),
            PaymentStatus = GetNullableString(data, "paymentStatus"),
            CancellationReason = GetNullableString(data, "cancellationReason", "reason")
        };
    }

    private PaymentReceivedModel CreatePaymentReceivedModel(Dictionary<string, object> data)
    {
        return new PaymentReceivedModel
        {
            Id = ParseInt(data, "paymentId"),
            InvoiceId = ParseInt(data, "invoiceId"),
            OrderHeaderId = ParseNullableInt(data, "orderId", "orderNumber"),
            Amount = ParseDecimal(data, "amount"),
            PaymentDate = ParseDateTime(data, "paymentDate") ?? DateTime.UtcNow,
            PaymentReference = GetNullableString(data, "paymentReference"),
            PaymentMethod = ParseEnum(data, "paymentMethod", PaymentMethod.Unknown),
            TransactionId = GetNullableString(data, "transactionId"),
            InvoiceNumber = GetString(data, "invoiceNumber"),
            CustomerName = GetString(data, "customerName", "name"),
            Currency = GetString(data, "currency", defaultValue: "NOK")
        };
    }

    private InvoiceGeneratedModel CreateInvoiceGeneratedModel(Dictionary<string, object> data)
    {
        return new InvoiceGeneratedModel
        {
            Id = ParseInt(data, "invoiceId"),
            OrderHeaderId = ParseNullableInt(data, "orderId", "orderNumber"),
            InvoiceNumber = GetString(data, "invoiceNumber"),
            KID = GetString(data, "kid"),
            IssueDate = ParseDateOnly(data, "issueDate"),
            DueDate = ParseDateOnly(data, "dueDate"),
            NetAmount = ParseDecimal(data, "netAmount"),
            VatAmount = ParseDecimal(data, "vatAmount"),
            TotalAmount = ParseDecimal(data, "totalAmount"),
            Currency = GetString(data, "currency", defaultValue: "NOK"),
            Status = ParseEnum(data, "status", InvoiceStatus.Draft),
            SellerName = GetString(data, "sellerName"),
            SellerOrgNumber = GetString(data, "sellerOrgNumber"),
            SellerAddress = GetNullableString(data, "sellerAddress"),
            SellerEmail = GetNullableString(data, "sellerEmail"),
            SellerPhone = GetNullableString(data, "sellerPhone"),
            CustomerName = GetString(data, "customerName", "name"),
            CustomerOrgNumber = GetNullableString(data, "customerOrgNumber"),
            CustomerAddress = GetNullableString(data, "customerAddress"),
            CustomerEmail = GetNullableString(data, "customerEmail"),
            BankAccountNumber = GetNullableString(data, "bankAccountNumber"),
            IBAN = GetNullableString(data, "iban"),
            BIC = GetNullableString(data, "bic"),
            SentDate = ParseNullableDateTime(data, "sentDate"),
            PdfUrl = GetNullableString(data, "pdfUrl"),
            PaidDate = ParseNullableDateTime(data, "paidDate"),
            TotalPaid = ParseDecimal(data, "totalPaid"),
            RemainingAmount = ParseDecimal(data, "remainingAmount")
        };
    }

    private ReturnRequestReceivedModel CreateReturnRequestReceivedModel(Dictionary<string, object> data)
    {
        return new ReturnRequestReceivedModel
        {
            Id = ParseInt(data, "returnRequestId"),
            OrderDetailId = ParseInt(data, "orderDetailId"),
            ApplicationUserId = GetString(data, "applicationUserId"),
            CustomerName = GetString(data, "customerName", "name"),
            Reason = GetString(data, "reason"),
            Description = GetNullableString(data, "description"),
            Quantity = ParseInt(data, "quantity"),
            RequestDate = ParseDateTime(data, "requestDate") ?? DateTime.UtcNow,
            Status = GetString(data, "status"),
            AdminNote = GetNullableString(data, "adminNote"),
            RefundAmount = ParseNullableDecimal(data, "refundAmount"),
            OrderHeaderId = ParseInt(data, "orderId", "orderNumber"),
            ProductName = GetNullableString(data, "productName")
        };
    }

    private ReturnRequestApprovedModel CreateReturnRequestApprovedModel(Dictionary<string, object> data)
    {
        return new ReturnRequestApprovedModel
        {
            Id = ParseInt(data, "returnRequestId"),
            OrderDetailId = ParseInt(data, "orderDetailId"),
            ApplicationUserId = GetString(data, "applicationUserId"),
            CustomerName = GetString(data, "customerName", "name"),
            Reason = GetString(data, "reason"),
            Description = GetNullableString(data, "description"),
            Quantity = ParseInt(data, "quantity"),
            RequestDate = ParseDateTime(data, "requestDate") ?? DateTime.UtcNow,
            Status = GetString(data, "status"),
            AdminNote = GetNullableString(data, "adminNote"),
            ResolvedDate = ParseNullableDateTime(data, "resolvedDate"),
            RefundAmount = ParseNullableDecimal(data, "refundAmount"),
            RefundId = GetNullableString(data, "refundId"),
            RefundDate = ParseNullableDateTime(data, "refundDate"),
            OrderHeaderId = ParseInt(data, "orderId", "orderNumber"),
            ProductName = GetNullableString(data, "productName")
        };
    }

    private ReturnRequestRejectedModel CreateReturnRequestRejectedModel(Dictionary<string, object> data)
    {
        return new ReturnRequestRejectedModel
        {
            Id = ParseInt(data, "returnRequestId"),
            OrderDetailId = ParseInt(data, "orderDetailId"),
            ApplicationUserId = GetString(data, "applicationUserId"),
            CustomerName = GetString(data, "customerName", "name"),
            Reason = GetString(data, "reason"),
            Description = GetNullableString(data, "description"),
            Quantity = ParseInt(data, "quantity"),
            RequestDate = ParseDateTime(data, "requestDate") ?? DateTime.UtcNow,
            Status = GetString(data, "status"),
            AdminNote = GetNullableString(data, "adminNote"),
            ResolvedDate = ParseNullableDateTime(data, "resolvedDate"),
            RefundAmount = ParseNullableDecimal(data, "refundAmount"),
            OrderHeaderId = ParseInt(data, "orderId", "orderNumber"),
            ProductName = GetNullableString(data, "productName")
        };
    }

    private PasswordResetTemplateModel CreatePasswordResetTemplateModel(Dictionary<string, object> data)
    {
        return new PasswordResetTemplateModel
        {
            UserId = data.TryGetValue("userId", out var userId) ? userId.ToString() ?? string.Empty : string.Empty,
            Name = data.TryGetValue("name", out var name)
                ? name.ToString() ?? ""
                : data.TryGetValue("userName", out var userName)
                    ? userName.ToString() ?? ""
                    : "",
            Email = data.TryGetValue("email", out var email) ? email.ToString() : null,
            IsActive = ParseBool(data, "isActive", true),
            ResetLink = data.TryGetValue("resetLink", out var link) ? link.ToString() ?? "" : "",
            ExpirationTime = data.TryGetValue("expirationTime", out var exp) ? exp.ToString() ?? "24 hours" : "24 hours"
        };
    }

    private WelcomeEmailTemplateModel CreateWelcomeEmailTemplateModel(Dictionary<string, object> data)
    {
        return new WelcomeEmailTemplateModel
        {
            UserId = data.TryGetValue("userId", out var userId) ? userId.ToString() ?? string.Empty : string.Empty,
            Name = data.TryGetValue("name", out var name)
                ? name.ToString() ?? ""
                : data.TryGetValue("userName", out var userName)
                    ? userName.ToString() ?? ""
                    : "",
            Email = data.TryGetValue("email", out var email) ? email.ToString() : null,
            IsActive = ParseBool(data, "isActive", true),
            VerificationLink = data.TryGetValue("verificationLink", out var link) ? link.ToString() : null
        };
    }

    private GenericNotificationModel CreateGenericNotificationModel(Notification notification, Dictionary<string, object> data)
    {
        return new GenericNotificationModel
        {
            Id = notification.Id,
            Type = notification.Type,
            Channel = notification.Channel,
            Status = notification.Status,
            Recipient = notification.Recipient,
            Subject = notification.Subject,
            Body = data.TryGetValue("body", out var body) ? body.ToString() : null,
            ErrorMessage = notification.ErrorMessage,
            RetryCount = notification.RetryCount,
            CreatedAt = notification.CreatedAt,
            ProcessedAt = notification.ProcessedAt,
            SentAt = notification.SentAt,
            UserId = notification.UserId,
            ReferenceId = notification.ReferenceId,
            ReferenceType = notification.ReferenceType
        };
    }

    private static int ParseInt(Dictionary<string, object> data, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (data.TryGetValue(key, out var value) && int.TryParse(value?.ToString(), out var result))
            {
                return result;
            }
        }

        return 0;
    }

    private static int? ParseNullableInt(Dictionary<string, object> data, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (data.TryGetValue(key, out var value) && int.TryParse(value?.ToString(), out var result))
            {
                return result;
            }
        }

        return null;
    }

    private static DateTime? ParseDateTime(Dictionary<string, object> data, string key)
    {
        return data.TryGetValue(key, out var value) && DateTime.TryParse(value?.ToString(), out var result)
            ? result
            : null;
    }

    private static DateTime? ParseDateTime(Dictionary<string, object> data, params string[] keys)
    {
        foreach (var key in keys)
        {
            var parsed = ParseDateTime(data, key);
            if (parsed.HasValue)
            {
                return parsed;
            }
        }

        return null;
    }

    private static DateTime? ParseNullableDateTime(Dictionary<string, object> data, string key)
    {
        return ParseDateTime(data, key);
    }

    private static DateOnly ParseDateOnly(Dictionary<string, object> data, string key)
    {
        return data.TryGetValue(key, out var value) && DateOnly.TryParse(value?.ToString(), out var result)
            ? result
            : DateOnly.MinValue;
    }

    private static decimal ParseDecimal(Dictionary<string, object> data, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (data.TryGetValue(key, out var value) && decimal.TryParse(value?.ToString(), out var result))
            {
                return result;
            }
        }

        return 0m;
    }

    private static decimal? ParseNullableDecimal(Dictionary<string, object> data, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (data.TryGetValue(key, out var value) && decimal.TryParse(value?.ToString(), out var result))
            {
                return result;
            }
        }

        return null;
    }

    private static TEnum ParseEnum<TEnum>(Dictionary<string, object> data, string key, TEnum defaultValue) where TEnum : struct
    {
        return data.TryGetValue(key, out var value) && Enum.TryParse<TEnum>(value?.ToString(), true, out var result)
            ? result
            : defaultValue;
    }

    private static string GetString(Dictionary<string, object> data, string key1, string? key2 = null, string defaultValue = "")
    {
        if (data.TryGetValue(key1, out var value) && !string.IsNullOrWhiteSpace(value?.ToString()))
        {
            return value!.ToString()!;
        }

        if (!string.IsNullOrWhiteSpace(key2) && data.TryGetValue(key2, out var value2) && !string.IsNullOrWhiteSpace(value2?.ToString()))
        {
            return value2!.ToString()!;
        }

        return defaultValue;
    }

    private static string? GetNullableString(Dictionary<string, object> data, string key1, string? key2 = null)
    {
        var value = GetString(data, key1, key2);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static bool ParseBool(Dictionary<string, object> data, string key, bool defaultValue)
    {
        return data.TryGetValue(key, out var value) && bool.TryParse(value?.ToString(), out var result)
            ? result
            : defaultValue;
    }
}
