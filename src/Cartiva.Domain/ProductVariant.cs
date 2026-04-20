using Cartiva.Domain;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ProductVariant
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Color is required.")]
    [StringLength(30)]
    public string Color { get; set; }

    // Nullable for products without sizes (accessories)
    public int? SizeValueId { get; set; }

    [ForeignKey("SizeValueId")]
    public SizeValue? SizeValue { get; set; }

    // =========================
    // PRICING WITH VAT BREAKDOWN
    // =========================

    /// <summary>
    /// Base price excluding VAT
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, 100000)]
    public decimal PriceExVat { get; set; }

    /// <summary>
    /// VAT rate as percentage (e.g., 25.00 for 25%)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    public decimal VatRate { get; set; } = 25.00m;

    /// <summary>
    /// Calculated VAT amount: PriceExVat * (VatRate / 100)
    /// </summary>
    [NotMapped]
    public decimal VatAmount => PriceExVat * (VatRate / 100m);

    /// <summary>
    /// Price including VAT (what customer pays): PriceExVat + VatAmount
    /// </summary>
    [NotMapped]
    public decimal PriceIncVat => PriceExVat + VatAmount;

    /// <summary>
    /// Legacy Price field - now maps to PriceIncVat for backward compatibility
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    [Range(1, 100000)]
    public decimal Price { get; set; }

    // =========================
    // DISCOUNT FIELDS
    // =========================

    /// <summary>
    /// Discount percentage (e.g., 10.00 for 10% off)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100)]
    public decimal DiscountPercent { get; set; } = 0;

    /// <summary>
    /// Calculated discount amount based on PriceIncVat
    /// </summary>
    [NotMapped]
    public decimal DiscountAmount => PriceIncVat * (DiscountPercent / 100m);

    /// <summary>
    /// Final price after discount (including VAT)
    /// </summary>
    [NotMapped]
    public decimal FinalPrice => PriceIncVat - DiscountAmount;

    /// <summary>
    /// Final price excluding VAT (after discount)
    /// </summary>
    [NotMapped]
    public decimal FinalPriceExVat => FinalPrice / (1 + VatRate / 100m);

    /// <summary>
    /// VAT amount on final price
    /// </summary>
    [NotMapped]
    public decimal FinalVatAmount => FinalPrice - FinalPriceExVat;

    /// <summary>
    /// Whether this variant has an active discount
    /// </summary>
    [NotMapped]
    public bool HasDiscount => DiscountPercent > 0;

    [Range(0, 1000)]
    public int Stock { get; set; }

    [Required]
    public int ProductId { get; set; }

    [ForeignKey("ProductId")]
    public Product Product { get; set; }

    public ICollection<Review>? Reviews { get; set; }

    // =========================
    // HELPER METHODS
    // =========================

    /// <summary>
    /// Sets price from an inclusive VAT amount (calculates PriceExVat automatically)
    /// </summary>
    public void SetPriceIncVat(decimal priceIncVat, decimal vatRate = 25.00m)
    {
        VatRate = vatRate;
        PriceExVat = priceIncVat / (1 + vatRate / 100m);
        Price = priceIncVat; // Keep legacy field in sync
    }

    /// <summary>
    /// Sets price from an exclusive VAT amount
    /// </summary>
    public void SetPriceExVat(decimal priceExVat, decimal vatRate = 25.00m)
    {
        VatRate = vatRate;
        PriceExVat = priceExVat;
        Price = PriceIncVat; // Keep legacy field in sync
    }
}