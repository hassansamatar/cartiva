using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cartiva.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExistingOrdersVatData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update OrderDetails with VAT breakdown from ProductVariant
            // Also populate ProductName and VariantDescription snapshots
            migrationBuilder.Sql(@"
                UPDATE od
                SET 
                    od.PriceExVat = CASE WHEN pv.PriceExVat > 0 THEN pv.PriceExVat ELSE od.Price / 1.25 END,
                    od.VatRate = CASE WHEN pv.VatRate > 0 THEN pv.VatRate ELSE 25.00 END,
                    od.PriceIncVat = od.Price,
                    od.ProductName = p.Name,
                    od.VariantDescription = pv.Color + CASE WHEN sv.Value IS NOT NULL THEN ' - ' + sv.Value ELSE '' END
                FROM OrderDetails od
                INNER JOIN ProductVariants pv ON od.ProductVariantId = pv.Id
                INNER JOIN Products p ON pv.ProductId = p.Id
                LEFT JOIN SizeValues sv ON pv.SizeValueId = sv.Id
                WHERE od.PriceExVat = 0 OR od.ProductName IS NULL
            ");

            // Update OrderHeaders with VAT totals calculated from OrderDetails
            migrationBuilder.Sql(@"
                UPDATE oh
                SET 
                    oh.SubtotalExVat = ISNULL(totals.SumExVat, oh.OrderTotal / 1.25),
                    oh.TotalVatAmount = ISNULL(totals.SumVat, oh.OrderTotal - (oh.OrderTotal / 1.25)),
                    oh.Currency = CASE WHEN oh.Currency IS NULL OR oh.Currency = '' THEN 'NOK' ELSE oh.Currency END
                FROM OrderHeaders oh
                LEFT JOIN (
                    SELECT 
                        OrderHeaderId,
                        SUM(PriceExVat * Count) as SumExVat,
                        SUM((PriceExVat * (VatRate / 100)) * Count) as SumVat
                    FROM OrderDetails
                    WHERE PriceExVat > 0
                    GROUP BY OrderHeaderId
                ) totals ON oh.Id = totals.OrderHeaderId
                WHERE oh.SubtotalExVat = 0 AND oh.OrderTotal > 0
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed - data enhancement only
        }
    }
}
