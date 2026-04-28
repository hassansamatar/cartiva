namespace Cartiva.Infrastructure.Notifications.Templates.Models;

public class OrderConfirmationModel
{
    public string OrderId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OrderDate { get; set; } = string.Empty;
    public string TotalAmount { get; set; } = string.Empty;
    public List<string> Items { get; set; } = new();
}

public class OrderShippedModel
{
    public string OrderId { get; set; } = string.Empty;
    public string TrackingNumber { get; set; } = string.Empty;
    public string Carrier { get; set; } = string.Empty;
    public string EstimatedDeliveryDate { get; set; } = string.Empty;

    public string OrderNumber
    {
        get => OrderId;
        set => OrderId = value;
    }
}

public class PasswordResetModel
{
    public string Name { get; set; } = string.Empty;
    public string ResetLink { get; set; } = string.Empty;
    public string ExpirationTime { get; set; } = string.Empty;
}

public class WelcomeEmailModel
{
    public string Name { get; set; } = string.Empty;
    public string? VerificationLink { get; set; }
}

public class GenericEmailModel
{
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public Dictionary<string, object> Data { get; set; } = new();
}

public class CreditNoteGeneratedModel
{
    public string CreditNoteNumber { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;
    public string TotalAmount { get; set; } = string.Empty;
    public string NetAmount { get; set; } = string.Empty;
    public string VatAmount { get; set; } = string.Empty;
    public string Currency { get; set; } = "NOK";
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ARAdjustmentNotificationModel
{
    public string CompanyName { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string Currency { get; set; } = "NOK";
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string? AppliedAt { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? Notes { get; set; }
}
