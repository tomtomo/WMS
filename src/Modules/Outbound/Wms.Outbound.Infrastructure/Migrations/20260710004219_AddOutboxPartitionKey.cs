using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Wms.Outbound.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxPartitionKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "partition_key",
                schema: "infrastructure",
                table: "outbox",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "partition_key",
                schema: "infrastructure",
                table: "outbox");
        }
    }
}
