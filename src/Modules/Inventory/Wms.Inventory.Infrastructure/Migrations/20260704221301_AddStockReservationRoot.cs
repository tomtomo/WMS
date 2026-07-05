using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockReservationRoot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_reservation",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_id = table.Column<Guid>(type: "uuid", nullable: false),
                    wave_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    batch = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    picking_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    release_reason = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_reservation", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_stock_reservation_wave_id",
                schema: "inventory",
                table: "stock_reservation",
                column: "wave_id");

            migrationBuilder.CreateIndex(
                name: "ix_stock_reservation_wave_id_order_id_sku",
                schema: "inventory",
                table: "stock_reservation",
                columns: new[] { "wave_id", "order_id", "sku" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_reservation",
                schema: "inventory");
        }
    }
}
