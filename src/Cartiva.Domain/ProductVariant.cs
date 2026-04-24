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

    [ForeignKey(nameof(SizeValueId))]
    public SizeValue? SizeValue { get; set; }

    // =========================
    // PRICING (SOURCE OF TRUTH)
    // =========================

    /// <summary>
    /// Base price excluding VAT (stored in DB)
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
    /// VAT amount
    /// </summary>
    [NotMapped]
    public decimal VatAmount =>
        Math.Round(PriceExVat * (VatRate / 100m), 2);

    /// <summary>
    /// Price including VAT
    /// </summary>
    [NotMapped]
    public decimal PriceIncVat =>
        Math.Round(PriceExVat + VatAmount, 2);

    /// <summary>
    /// Backward-compatible price (used across the app)
    /// NOT stored in DB
    /// </summary>
    [NotMapped]
    public decimal Price => PriceIncVat;

    // =========================
    // DISCOUNTS
    // =========================

    /// <summary>
    /// Discount percentage (0–100)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100)]
    public decimal DiscountPercent { get; set; } = 0;

    /// <summary>
    /// Discount amount (based on VAT-inclusive price)
    /// </summary>
    [NotMapped]
    public decimal DiscountAmount =>
        Math.Round(PriceIncVat * (DiscountPercent / 100m), 2);

    /// <summary>
    /// Final price including VAT after discount
    /// </summary>
    [NotMapped]
    public decimal FinalPrice =>
        Math.Round(PriceIncVat - DiscountAmount, 2);

    /// <summary>
    /// Final price excluding VAT after discount
    /// </summary>
    [NotMapped]
    public decimal FinalPriceExVat =>
        Math.Round(FinalPrice / (1 + VatRate / 100m), 2);

    /// <summary>
    /// VAT amount on final price
    /// </summary>
    [NotMapped]
    public decimal FinalVatAmount =>
        Math.Round(FinalPrice - FinalPriceExVat, 2);

    /// <summary>
    /// Whether discount is active
    /// </summary>
    [NotMapped]
    public bool HasDiscount => DiscountPercent > 0;

    // =========================
    // INVENTORY & RELATIONS
    // =========================

    [Range(0, 1000)]
    public int Stock { get; set; }

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product Product { get; set; }

    public ICollection<Review>? Reviews { get; set; }

    // =========================
    // HELPER METHODS
    // =========================

    /// <summary>
    /// Set price from VAT-inclusive value
    /// </summary>
    public void SetPriceIncVat(decimal priceIncVat, decimal vatRate = 25.00m)
    {
        VatRate = vatRate;
        PriceExVat = Math.Round(priceIncVat / (1 + vatRate / 100m), 2);
    }

    /// <summary>
    /// Set price from VAT-exclusive value
    /// </summary>
    public void SetPriceExVat(decimal priceExVat, decimal vatRate = 25.00m)
    {
        VatRate = vatRate;
        PriceExVat = Math.Round(priceExVat, 2);
    }
}