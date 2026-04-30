namespace Cartiva.Infrastructure.Templates.Models;

public class ARAdjustmentEmailModel
{
    public string AdjustmentId { get; set; } = string.Empty;
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
