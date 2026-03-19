using Models;
using System.ComponentModel.DataAnnotations.Schema;

public class ShoppingCart
{
    public int Id { get; set; }
    public string ApplicationUserId { get; set; }
    public int ProductVariantId { get; set; }
    public int Count { get; set; }

    // NEW: Tracking properties
    public DateTime? DateAdded { get; set; }
    public DateTime? LastUpdated { get; set; }

    [ForeignKey("ApplicationUserId")]
    public ApplicationUser ApplicationUser { get; set; }

    [ForeignKey("ProductVariantId")]
    public ProductVariant ProductVariant { get; set; }
}