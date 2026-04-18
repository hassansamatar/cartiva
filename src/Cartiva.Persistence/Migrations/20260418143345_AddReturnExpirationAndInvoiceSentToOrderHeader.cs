using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cartiva.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnExpirationAndInvoiceSentToOrderHeader : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InvoiceSent",
                table: "OrderHeaders",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnExpirationDate",
                table: "OrderHeaders",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceSent",
                table: "OrderHeaders");

            migrationBuilder.DropColumn(
                name: "ReturnExpirationDate",
                table: "OrderHeaders");
        }
    }
}
