using Cartiva.Domain;

namespace Cartiva.Infrastructure.Templates.Models;

public class PaymentReceivedModel
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int? OrderHeaderId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? PaymentReference { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Unknown;
    public string? TransactionId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Currency { get; set; } = "NOK";
}
