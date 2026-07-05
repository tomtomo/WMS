using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Infrastructure;

namespace Wms.Outbound.Infrastructure;

// DbContext modul Outbound — schema 'outbound'
public sealed class OutboundDbContext(DbContextOptions<OutboundDbContext> options) : DbContext(options)
{
    public const string Schema = "outbound";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // Outbox/Inbox/DLQ/audit — satu transaksi dengan state modul
        modelBuilder.AddInfrastructureTables();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OutboundDbContext).Assembly);
    }
}
