namespace Cartiva.Infrastructure.Templates.Models;

public class OrderConfirmationModel
{
    public string OrderId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string OrderDate { get; set; } = string.Empty;
    public string TotalAmount { get; set; } = string.Empty;
    public List<string> Items { get; set; } = new();
}
