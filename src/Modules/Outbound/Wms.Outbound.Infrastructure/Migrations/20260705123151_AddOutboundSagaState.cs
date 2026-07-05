using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Outbound.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboundSagaState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "saga_state",
                schema: "outbound",
                columns: table => new
                {
                    saga_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    saga_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    state = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_saga_state", x => x.saga_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_saga_state_saga_type",
                schema: "outbound",
                table: "saga_state",
                column: "saga_type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "saga_state",
                schema: "outbound");
        }
    }
}
