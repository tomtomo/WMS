using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockReservationClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "reservations",
                schema: "inventory",
                table: "stock",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "reservations",
                schema: "inventory",
                table: "stock");
        }
    }
}
