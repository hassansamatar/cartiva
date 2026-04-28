namespace Cartiva.Infrastructure.Templates.Models;

public class ARAdjustmentNotificationModel
{
    public string AdjustmentId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Amount { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string AppliedAt { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}