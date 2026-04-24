namespace Cartiva.Infrastructure.Templates.Models;

public class OrderCancelledModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal OrderTotal { get; set; }
    public string Currency { get; set; } = "NOK";
    public string? OrderStatus { get; set; }
    public string? PaymentStatus { get; set; }
    public string? CancellationReason { get; set; }
}
