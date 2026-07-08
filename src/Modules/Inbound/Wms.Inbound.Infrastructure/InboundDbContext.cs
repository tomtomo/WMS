using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure;
using Wms.Inbound.Domain;

namespace Wms.Inbound.Infrastructure;

// DbContext modul Inbound — schema 'inbound'. Warehouse scoping  baca ICurrentUser
public sealed class InboundDbContext : DbContext
{
    public const string Schema = "inbound";

    // Snapshot scope warehouse dari current user.
    private readonly bool _bypassWarehouseScope;
    private readonly Guid[] _scopedWarehouseIds;

    public InboundDbContext(DbContextOptions<InboundDbContext> options, ICurrentUser? currentUser = null)
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

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(InboundDbContext).Assembly);

        // Filter akses warehouse berdasarkan current user.
        modelBuilder.Entity<GoodsReceipt>().HasQueryFilter(
            goodsReceipt => _bypassWarehouseScope || _scopedWarehouseIds.Contains(goodsReceipt.WarehouseId));
    }
}
