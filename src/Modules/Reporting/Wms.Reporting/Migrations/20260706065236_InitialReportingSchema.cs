using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Reporting.Migrations
{
    /// <inheritdoc />
    public partial class InitialReportingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "reporting");

            migrationBuilder.EnsureSchema(
                name: "infrastructure");

            migrationBuilder.CreateTable(
                name: "dispatch_summary",
                schema: "reporting",
                columns: table => new
                {
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    dispatched_volume = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    wave_throughput = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dispatch_summary", x => new { x.warehouse_id, x.period });
                });

            migrationBuilder.CreateTable(
                name: "inbox",
                schema: "infrastructure",
                columns: table => new
                {
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    handler_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inbox", x => new { x.event_id, x.handler_type });
                });

            migrationBuilder.CreateTable(
                name: "operator_activity",
                schema: "reporting",
                columns: table => new
                {
                    operator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    putaway_count = table.Column<int>(type: "integer", nullable: false),
                    pick_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operator_activity", x => new { x.operator_id, x.period });
                });

            migrationBuilder.CreateTable(
                name: "receiving_summary",
                schema: "reporting",
                columns: table => new
                {
                    supplier_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period = table.Column<DateOnly>(type: "date", nullable: false),
                    received_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    receipt_count = table.Column<int>(type: "integer", nullable: false),
                    discrepancy_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receiving_summary", x => new { x.supplier_id, x.period });
                });

            migrationBuilder.CreateTable(
                name: "stock_on_hand_view",
                schema: "reporting",
                columns: table => new
                {
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    batch = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    qty_on_hand = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock_on_hand_view", x => new { x.warehouse_id, x.sku, x.batch });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "dispatch_summary",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "inbox",
                schema: "infrastructure");

            migrationBuilder.DropTable(
                name: "operator_activity",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "receiving_summary",
                schema: "reporting");

            migrationBuilder.DropTable(
                name: "stock_on_hand_view",
                schema: "reporting");
        }
    }
}
