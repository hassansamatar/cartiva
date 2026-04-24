namespace Cartiva.Infrastructure.Templates.Models;

public class ReturnRequestApprovedModel
{
    public int Id { get; set; }
    public int OrderDetailId { get; set; }
    public string ApplicationUserId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Quantity { get; set; }
    public DateTime RequestDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? AdminNote { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public decimal? RefundAmount { get; set; }
    public string? RefundId { get; set; }
    public DateTime? RefundDate { get; set; }
    public int OrderHeaderId { get; set; }
    public string? ProductName { get; set; }
}
