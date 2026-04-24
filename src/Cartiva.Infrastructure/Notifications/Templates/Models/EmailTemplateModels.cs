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
