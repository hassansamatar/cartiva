using Cartiva.Domain;

namespace Cartiva.Infrastructure.Templates.Models;

public class InvoiceGeneratedModel
{
    public int Id { get; set; }
    public int? OrderHeaderId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string KID { get; set; } = string.Empty;
    public DateOnly IssueDate { get; set; }
    public DateOnly DueDate { get; set; }
    public decimal NetAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "NOK";
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string SellerName { get; set; } = string.Empty;
    public string SellerOrgNumber { get; set; } = string.Empty;
    public string? SellerAddress { get; set; }
    public string? SellerEmail { get; set; }
    public string? SellerPhone { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerOrgNumber { get; set; }
    public string? CustomerAddress { get; set; }
    public string? CustomerEmail { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? IBAN { get; set; }
    public string? BIC { get; set; }
    public DateTime? SentDate { get; set; }
    public string? PdfUrl { get; set; }
    public DateTime? PaidDate { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal RemainingAmount { get; set; }
}
