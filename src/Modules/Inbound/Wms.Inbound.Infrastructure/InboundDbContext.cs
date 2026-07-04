using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Infrastructure;

namespace Wms.Inbound.Infrastructure;

// DbContext modul Inbound — schema 'inbound'
public sealed class InboundDbContext(DbContextOptions<InboundDbContext> options) : DbContext(options)
{
    public const string Schema = "inbound";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // Outbox/Inbox/DLQ/audit — satu transaksi dengan state modul.
        modelBuilder.AddInfrastructureTables();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InboundDbContext).Assembly);
    }
}
