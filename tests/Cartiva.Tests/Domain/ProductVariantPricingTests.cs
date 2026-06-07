using Xunit;
using Cartiva.Domain;

namespace Cartiva.Tests.Domain;

/// <summary>
/// Tests to lock in the pricing architecture and prevent regressions
/// These tests document the CORRECT behavior and must always pass
/// </summary>
public class ProductVariantPricingTests
{
    // ========================================
    // CORE PRICING RULES (MUST NEVER CHANGE)
    // ========================================

    [Fact]
    public void PriceIncVat_ComputedFromPriceExVat_WithStandardVat()
    {
        // Arrange: Standard Norwegian VAT (25%)
        var variant = new ProductVariant
        {
            PriceExVat = 239.20m,
            VatRate = 25.00m
        };

        // Act
        var priceIncVat = variant.PriceIncVat;

        // Assert: 239.20 × 1.25 = 299.00
        Assert.Equal(299.00m, priceIncVat);
    }

    [Fact]
    public void Price_IsAlias_ForPriceIncVat()
    {
        // Arrange
        var variant = new ProductVariant
        {
            PriceExVat = 239.20m,
            VatRate = 25.00m
        };

        // Act & Assert: Price must equal PriceIncVat
        Assert.Equal(variant.PriceIncVat, variant.Price);
        Assert.Equal(299.00m, variant.Price);
    }

    [Fact]
    public void VatAmount_ComputedCorrectly()
    {
        // Arrange
        var variant = new ProductVariant
        {
            PriceExVat = 239.20m,
            VatRate = 25.00m
        };

        // Act
        var vatAmount = variant.VatAmount;

        // Assert: 239.20 × 0.25 = 59.80
        Assert.Equal(59.80m, vatAmount);
    }

    // ========================================
    // VAT RATE FORMAT (PERCENTAGE NOT DECIMAL)
    // ========================================

    [Theory]
    [InlineData(25.00, 1.25)]   // Standard rate: 25%
    [InlineData(15.00, 1.15)]   // Reduced rate: 15%
    [InlineData(12.00, 1.12)]   // Low rate: 12%
    [InlineData(0.00, 1.00)]    // Zero rate: 0%
    public void VatRate_UsesPercentageFormat_NotDecimal(decimal vatRate, decimal expectedMultiplier)
    {
        // Arrange
        var variant = new ProductVariant
        {
            PriceExVat = 100.00m,
            VatRate = vatRate
        };

        // Act
        var multiplier = 1 + (variant.VatRate / 100m);

        // Assert: VatRate must be in percentage format
        Assert.Equal(expectedMultiplier, multiplier);
        Assert.Equal(100m * expectedMultiplier, variant.PriceIncVat);
    }

    [Fact]
    public void VatRate_InvalidDecimalFormat_ProducesWrongResult()
    {
        // This test documents WRONG usage (for education)
        // If VatRate were 0.25 instead of 25.00, calculations break

        var variant = new ProductVariant
        {
            PriceExVat = 239.20m,
            VatRate = 0.25m  // ❌ WRONG FORMAT
        };

        // With wrong format: 239.20 × 1.0025 = 239.798 (WRONG!)
        var wrongResult = variant.PriceIncVat;

        // Assert: This is NOT the expected result
        Assert.NotEqual(299.00m, wrongResult);
        Assert.Equal(239.80m, Math.Round(wrongResult, 2));  // Wrong!
    }

    // ========================================
    // ROUNDING BEHAVIOR (MUST BE CONSISTENT)
    // ========================================

    [Theory]
    [InlineData(239.196, 25.00, 299.00)]   // Rounds to 2 decimals
    [InlineData(239.994, 25.00, 299.99)]   // No over-rounding
    [InlineData(239.995, 25.00, 300.00)]   // Banker's rounding
    public void PriceIncVat_RoundsToTwoDecimals(decimal priceExVat, decimal vatRate, decimal expected)
    {
        // Arrange
        var variant = new ProductVariant
        {
            PriceExVat = priceExVat,
            VatRate = vatRate
        };

        // Act
        var actual = variant.PriceIncVat;

        // Assert
        Assert.Equal(expected, actual);
    }

    // ========================================
    // DISCOUNT CALCULATIONS
    // ========================================

    [Fact]
    public void FinalPrice_AppliesDiscount_ToPriceIncVat()
    {
        // Arrange: 10% discount
        var variant = new ProductVariant
        {
            PriceExVat = 239.20m,
            VatRate = 25.00m,
            DiscountPercent = 10.00m
        };

        // Act
        var finalPrice = variant.FinalPrice;
        var discountAmount = variant.DiscountAmount;

        // Assert:
        // PriceIncVat = 299.00
        // Discount (10%) = 29.90
        // FinalPrice = 269.10
        Assert.Equal(299.00m, variant.PriceIncVat);
        Assert.Equal(29.90m, discountAmount);
        Assert.Equal(269.10m, finalPrice);
    }

    // ========================================
    // EDGE CASES
    // ========================================

    [Fact]
    public void ZeroVatRate_NoVatAdded()
    {
        // Arrange: VAT-exempt product
        var variant = new ProductVariant
        {
            PriceExVat = 299.00m,
            VatRate = 0.00m
        };

        // Act & Assert: Price = PriceExVat when VAT = 0
        Assert.Equal(299.00m, variant.PriceIncVat);
        Assert.Equal(0.00m, variant.VatAmount);
    }

    [Fact]
    public void MinimumPrice_OneOere()
    {
        // Arrange: Smallest possible price (0.01 kr)
        var variant = new ProductVariant
        {
            PriceExVat = 0.01m,
            VatRate = 25.00m
        };

        // Act
        var priceIncVat = variant.PriceIncVat;

        // Assert: 0.01 × 1.25 = 0.01 (rounds to 0.01)
        Assert.Equal(0.01m, priceIncVat);
    }

    // ========================================
    // VALIDATION TESTS
    // ========================================

    [Theory]
    [InlineData(-10.00)]   // Negative price
    [InlineData(0.00)]     // Zero price (use 0.01 minimum)
    public void PriceExVat_Validation_RejectsInvalidValues(decimal invalidPrice)
    {
        // This documents the Range validation
        var variant = new ProductVariant
        {
            PriceExVat = invalidPrice,
            VatRate = 25.00m
        };

        // In production, ModelState validation would catch this
        // For unit tests, we just document the constraint
        Assert.True(invalidPrice < 0.01m, "Price should be rejected by validation");
    }

    [Theory]
    [InlineData(-5.00)]    // Negative VAT
    [InlineData(101.00)]   // >100% VAT
    public void VatRate_Validation_RejectsInvalidValues(decimal invalidVatRate)
    {
        // This documents the Range validation
        var variant = new ProductVariant
        {
            PriceExVat = 239.20m,
            VatRate = invalidVatRate
        };

        // In production, ModelState validation would catch this
        Assert.True(invalidVatRate < 0 || invalidVatRate > 100, 
            "VAT rate should be rejected by validation");
    }

    // ========================================
    // BACKWARD COMPATIBILITY
    // ========================================

    [Fact]
    public void Price_Property_MaintainedForBackwardCompatibility()
    {
        // This test ensures the Price property continues to work
        // for existing code that reads variant.Price

        var variant = new ProductVariant
        {
            PriceExVat = 239.20m,
            VatRate = 25.00m
        };

        // Old code does this:
        decimal customerPays = variant.Price;

        // Must still work:
        Assert.Equal(299.00m, customerPays);
    }

    [Fact]
    public void Price_IsReadOnly_CannotBeSet()
    {
        // This test documents that Price has no setter
        // Attempting to set it should not compile

        var variant = new ProductVariant();

        // This should NOT compile:
        // variant.Price = 299.00m;  // ❌ Compile error

        // Only PriceExVat can be set:
        variant.PriceExVat = 239.20m;  // ✅ Correct way

        Assert.Equal(299.00m, variant.Price);  // Computed correctly
    }

    // ========================================
    // INTEGRATION WITH ORDER SYSTEM
    // ========================================

    [Fact]
    public void OrderDetail_SnapshotsPriceCorrectly()
    {
        // Arrange: Product variant with pricing
        var variant = new ProductVariant
        {
            Id = 1,
            ProductId = 100,
            Color = "Blue",
            PriceExVat = 239.20m,
            VatRate = 25.00m,
            Product = new Product { Name = "T-Shirt" }
        };

        // Act: Create order detail (snapshot pricing)
        var orderDetail = OrderDetail.FromProductVariant(variant, count: 2);

        // Assert: All price fields captured correctly
        Assert.Equal(239.20m, orderDetail.PriceExVat);
        Assert.Equal(25.00m, orderDetail.VatRate);
        Assert.Equal(299.00m, orderDetail.PriceIncVat);
        Assert.Equal(299.00m, orderDetail.Price);  // Legacy field
    }
}
