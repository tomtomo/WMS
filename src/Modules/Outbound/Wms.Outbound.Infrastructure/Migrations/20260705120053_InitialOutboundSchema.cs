using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Wms.Outbound.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialOutboundSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "infrastructure");

            migrationBuilder.EnsureSchema(
                name: "outbound");

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
                name: "outbound_order",
                schema: "outbound",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ship_to_recipient = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ship_to_address_line = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    ship_to_city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    wave_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbound_order", x => x.id);
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
                name: "picking_task",
                schema: "outbound",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    wave_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reservation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stock_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_location_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    batch = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    assigned_to = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    actual_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: true),
                    staging_location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_picking_task", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wave",
                schema: "outbound",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    warehouse_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cancel_reason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    modified_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    modified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    order_ids = table.Column<string>(type: "jsonb", nullable: false),
                    picking_task_ids = table.Column<string>(type: "jsonb", nullable: false),
                    reservation_ids = table.Column<string>(type: "jsonb", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_wave", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbound_backorder",
                schema: "outbound",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    short_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    outbound_order_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbound_backorder", x => x.id);
                    table.ForeignKey(
                        name: "fk_outbound_backorder_outbound_order_outbound_order_id",
                        column: x => x.outbound_order_id,
                        principalSchema: "outbound",
                        principalTable: "outbound_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "outbound_order_line",
                schema: "outbound",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    uom = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    allocated_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    allocation_status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    outbound_order_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbound_order_line", x => x.id);
                    table.ForeignKey(
                        name: "fk_outbound_order_line_outbound_order_outbound_order_id",
                        column: x => x.outbound_order_id,
                        principalSchema: "outbound",
                        principalTable: "outbound_order",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outbound_backorder_outbound_order_id",
                schema: "outbound",
                table: "outbound_backorder",
                column: "outbound_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbound_order_status",
                schema: "outbound",
                table: "outbound_order",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_outbound_order_wave_id",
                schema: "outbound",
                table: "outbound_order",
                column: "wave_id");

            migrationBuilder.CreateIndex(
                name: "ix_outbound_order_line_outbound_order_id",
                schema: "outbound",
                table: "outbound_order_line",
                column: "outbound_order_id");

            migrationBuilder.CreateIndex(
                name: "ix_picking_task_assigned_to",
                schema: "outbound",
                table: "picking_task",
                column: "assigned_to");

            migrationBuilder.CreateIndex(
                name: "ix_picking_task_wave_id_reservation_id",
                schema: "outbound",
                table: "picking_task",
                columns: new[] { "wave_id", "reservation_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_wave_warehouse_id_status",
                schema: "outbound",
                table: "wave",
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
                name: "outbound_backorder",
                schema: "outbound");

            migrationBuilder.DropTable(
                name: "outbound_order_line",
                schema: "outbound");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "infrastructure");

            migrationBuilder.DropTable(
                name: "picking_task",
                schema: "outbound");

            migrationBuilder.DropTable(
                name: "wave",
                schema: "outbound");

            migrationBuilder.DropTable(
                name: "outbound_order",
                schema: "outbound");
        }
    }
}
