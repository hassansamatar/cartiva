using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cartiva.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameIsInactiveToIsActiveIAppUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsInactive",
                table: "AspNetUsers",
                newName: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "AspNetUsers",
                newName: "IsInactive");
        }
    }
}
