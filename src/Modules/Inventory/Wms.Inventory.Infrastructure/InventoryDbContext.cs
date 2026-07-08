using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure;
using Wms.Inventory.Domain;

namespace Wms.Inventory.Infrastructure;

// DbContext modul Inventory — schema 'inventory'. Warehouse scoping  baca ICurrentUser
public sealed class InventoryDbContext : DbContext
{
    public const string Schema = "inventory";

    // Snapshot scope warehouse dari current user.
    private readonly bool _bypassWarehouseScope;
    private readonly Guid[] _scopedWarehouseIds;

    public InventoryDbContext(DbContextOptions<InventoryDbContext> options, ICurrentUser? currentUser = null)
        : base(options)
    {
        _bypassWarehouseScope = currentUser?.CanBypassWarehouseScope ?? true;
        _scopedWarehouseIds = currentUser?.AssignedWarehouseIds.ToArray() ?? [];
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // Outbox/Inbox/DLQ/audit — satu transaksi dengan state modul.
        modelBuilder.AddInfrastructureTables();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);

        // Filter akses warehouse berdasarkan current user.
        modelBuilder.Entity<Stock>().HasQueryFilter(
            stock => _bypassWarehouseScope || _scopedWarehouseIds.Contains(stock.WarehouseId));
    }
}
