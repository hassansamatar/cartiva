using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cartiva.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVatPricingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "Shipments",
                type: "decimal(10,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "ProductVariants",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceExVat",
                table: "ProductVariants",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "ProductVariants",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "OrderHeaders",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingCostExVat",
                table: "OrderHeaders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ShippingVatAmount",
                table: "OrderHeaders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalExVat",
                table: "OrderHeaders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDiscountAmount",
                table: "OrderHeaders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalVatAmount",
                table: "OrderHeaders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercent",
                table: "OrderDetails",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceExVat",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PriceIncVat",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ProductName",
                table: "OrderDetails",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "UnitDiscountAmount",
                table: "OrderDetails",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "VariantDescription",
                table: "OrderDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "OrderDetails",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            // =========================
            // DATA MIGRATION: Populate new fields from existing Price data
            // Assumes existing Price is inclusive of 25% VAT
            // =========================

            // ProductVariants: Calculate PriceExVat from existing Price (assuming Price = PriceIncVat)
            migrationBuilder.Sql(@"
                UPDATE ProductVariants 
                SET VatRate = 25.00,
                    PriceExVat = Price / 1.25,
                    DiscountPercent = 0
                WHERE PriceExVat = 0 AND Price > 0
            ");

            // OrderDetails: Calculate VAT breakdown from existing Price
            migrationBuilder.Sql(@"
                UPDATE OrderDetails 
                SET VatRate = 25.00,
                    PriceIncVat = Price,
                    PriceExVat = Price / 1.25,
                    DiscountPercent = 0,
                    UnitDiscountAmount = 0
                WHERE PriceIncVat = 0 AND Price > 0
            ");

            // OrderHeaders: Calculate VAT totals from existing OrderTotal
            migrationBuilder.Sql(@"
                UPDATE OrderHeaders 
                SET Currency = 'NOK',
                    SubtotalExVat = OrderTotal / 1.25,
                    TotalVatAmount = OrderTotal - (OrderTotal / 1.25),
                    TotalDiscountAmount = 0,
                    ShippingCostExVat = 0,
                    ShippingVatAmount = 0
                WHERE SubtotalExVat = 0 AND OrderTotal > 0
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "PriceExVat",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "OrderHeaders");

            migrationBuilder.DropColumn(
                name: "ShippingCostExVat",
                table: "OrderHeaders");

            migrationBuilder.DropColumn(
                name: "ShippingVatAmount",
                table: "OrderHeaders");

            migrationBuilder.DropColumn(
                name: "SubtotalExVat",
                table: "OrderHeaders");

            migrationBuilder.DropColumn(
                name: "TotalDiscountAmount",
                table: "OrderHeaders");

            migrationBuilder.DropColumn(
                name: "TotalVatAmount",
                table: "OrderHeaders");

            migrationBuilder.DropColumn(
                name: "DiscountPercent",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "PriceExVat",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "PriceIncVat",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ProductName",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "UnitDiscountAmount",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "VariantDescription",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "OrderDetails");

            migrationBuilder.AlterColumn<decimal>(
                name: "Weight",
                table: "Shipments",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldNullable: true);
        }
    }
}
