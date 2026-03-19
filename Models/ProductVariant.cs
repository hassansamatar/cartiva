using Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ProductVariant
{
    public int Id { get; set; }

    [Required]
    public string Color { get; set; }

    // Nullable for products without sizes (accessories)
    public int? SizeValueId { get; set; }  // Note: [Required] removed since it's nullable

    [ForeignKey("SizeValueId")]
    public SizeValue? SizeValue { get; set; }

    [Range(1, 100000)]
    public decimal Price { get; set; }

    [Range(0, 1000)]
    public int Stock { get; set; }

    [Required]
    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public Product Product { get; set; }  // Required - non-nullable
}