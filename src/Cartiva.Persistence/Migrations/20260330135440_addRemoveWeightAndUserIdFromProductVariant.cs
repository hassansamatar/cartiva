using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cartiva.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addRemoveWeightAndUserIdFromProductVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "ProductVariants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "ProductVariants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "ProductVariants",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
