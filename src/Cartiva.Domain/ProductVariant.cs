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
    /// SOURCE OF TRUTH for all pricing calculations
    /// </summary>
    [Column(TypeName = "decimal(18,2)")]
    [Range(0.01, 100000, ErrorMessage = "Price must be between 0.01 and 100,000")]
    public decimal PriceExVat { get; set; }

    /// <summary>
    /// VAT rate as percentage (e.g., 25.00 for 25%)
    /// IMPORTANT: Use percentage format (25 not 0.25)
    /// </summary>
    [Column(TypeName = "decimal(5,2)")]
    [Range(0, 100, ErrorMessage = "VAT rate must be between 0 and 100")]
    public decimal VatRate { get; set; } = 25.00m;

    /// <summary>
    /// VAT amount (25% of total price)
    /// </summary>
    [NotMapped]
    public decimal VatAmount =>
        Math.Round(PriceIncVat * (VatRate / 100m), 2);

    /// <summary>
    /// Price including VAT
    /// Formula: PriceExVat represents 75% of total when VAT=25%
    /// </summary>
    [NotMapped]
    public decimal PriceIncVat =>
        Math.Round(PriceExVat / (1 - VatRate / 100m), 2);

    /// <summary>
    /// Backward-compatible price property (VAT-inclusive)
    /// READ-ONLY - computed from PriceExVat + VatRate
    /// 
    /// IMPORTANT: This property exists ONLY for backward compatibility
    /// with existing read-only usages (cart displays, product lists, etc.)
    /// 
    /// For form binding, use PriceExVat directly in ViewModels.
    /// For display purposes, use PriceIncVat (more explicit).
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
    // HELPER METHODS (WITH VALIDATION)
    // =========================

    /// <summary>
    /// Set price from VAT-inclusive value (what customer pays)
    /// Guards against invalid VAT rates and negative prices
    /// </summary>
    public void SetPriceIncVat(decimal priceIncVat, decimal vatRate = 25.00m)
    {
        ValidatePrice(priceIncVat, nameof(priceIncVat));
        ValidateVatRate(vatRate, nameof(vatRate));

        VatRate = vatRate;
        PriceExVat = Math.Round(priceIncVat / (1 + vatRate / 100m), 2);
    }

    /// <summary>
    /// Set price from VAT-exclusive value (base price before VAT)
    /// Guards against invalid VAT rates and negative prices
    /// </summary>
    public void SetPriceExVat(decimal priceExVat, decimal vatRate = 25.00m)
    {
        ValidatePrice(priceExVat, nameof(priceExVat));
        ValidateVatRate(vatRate, nameof(vatRate));

        VatRate = vatRate;
        PriceExVat = Math.Round(priceExVat, 2);
    }

    // =========================
    // VALIDATION GUARDS
    // =========================

    /// <summary>
    /// Validates price is positive and within range
    /// </summary>
    private static void ValidatePrice(decimal price, string paramName)
    {
        if (price < 0.01m)
            throw new ArgumentException("Price must be at least 0.01", paramName);
        if (price > 100000m)
            throw new ArgumentException("Price cannot exceed 100,000", paramName);
    }

    /// <summary>
    /// Validates VAT rate is in percentage format (0-100)
    /// Prevents common mistake of using decimal format (e.g., 0.25 instead of 25)
    /// </summary>
    private static void ValidateVatRate(decimal vatRate, string paramName)
    {
        if (vatRate < 0)
            throw new ArgumentException("VAT rate cannot be negative", paramName);
        if (vatRate > 100)
            throw new ArgumentException("VAT rate cannot exceed 100%", paramName);

        // Guard against common mistake: decimal format (0.25) instead of percentage (25)
        if (vatRate > 0 && vatRate < 1)
            throw new ArgumentException(
                $"VAT rate appears to be in decimal format ({vatRate}). " +
                $"Use percentage format instead (e.g., 25 for 25%, not 0.25)", 
                paramName);
    }
}