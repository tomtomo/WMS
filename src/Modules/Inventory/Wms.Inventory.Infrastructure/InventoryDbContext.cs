using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Infrastructure;

namespace Wms.Inventory.Infrastructure;

// DbContext modul Inventory — schema 'inventory'
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public const string Schema = "inventory";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // Outbox/Inbox/DLQ/audit — satu transaksi dengan state modul.
        modelBuilder.AddInfrastructureTables();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
    }
}
