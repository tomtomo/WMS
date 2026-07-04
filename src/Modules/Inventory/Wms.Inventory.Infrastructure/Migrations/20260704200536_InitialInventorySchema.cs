using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialInventorySchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "infrastructure");

            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "audit_log",
                schema: "infrastructure",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    action = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    entity = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    payload = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "dead_letter",
                schema: "infrastructure",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    error = table.Column<string>(type: "text", nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    dead_lettered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_dead_letter", x => x.id);
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
                name: "outbox",
                schema: "infrastructure",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    logical_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    delivery_class = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    payload = table.Column<string>(type: "text", nullable: false),
                    traceparent = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    tracestate = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "putaway_task",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    suggested_destination_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_to = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    actual_destination_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_putaway_task", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stock",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    batch = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expiry = table.Column<DateOnly>(type: "date", nullable: false),
                    qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    source_gr_id = table.Column<Guid>(type: "uuid", nullable: false),
                    line = table.Column<int>(type: "integer", nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    picking_task_id = table.Column<Guid>(type: "uuid", nullable: true),
                    wave_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_stock", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_putaway_task_status_assigned_to",
                schema: "inventory",
                table: "putaway_task",
                columns: new[] { "status", "assigned_to" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_source_gr_id_line",
                schema: "inventory",
                table: "stock",
                columns: new[] { "source_gr_id", "line" },
                unique: true,
                filter: "status <> 'Picked'");

            migrationBuilder.CreateIndex(
                name: "ix_stock_warehouse_id_status",
                schema: "inventory",
                table: "stock",
                columns: new[] { "warehouse_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_log",
                schema: "infrastructure");

            migrationBuilder.DropTable(
                name: "dead_letter",
                schema: "infrastructure");

            migrationBuilder.DropTable(
                name: "inbox",
                schema: "infrastructure");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "infrastructure");

            migrationBuilder.DropTable(
                name: "putaway_task",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "stock",
                schema: "inventory");
        }
    }
}
