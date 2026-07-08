using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure;
using Wms.Outbound.Domain;

namespace Wms.Outbound.Infrastructure;

// DbContext modul Outbound — schema 'outbound'. scope warehouse diterapkan di Wave.
public sealed class OutboundDbContext : DbContext
{
    public const string Schema = "outbound";

    // Snapshot scope warehouse dari current user.
    private readonly bool _bypassWarehouseScope;
    private readonly Guid[] _scopedWarehouseIds;

    public OutboundDbContext(DbContextOptions<OutboundDbContext> options, ICurrentUser? currentUser = null)
        : base(options)
    {
        _bypassWarehouseScope = currentUser?.CanBypassWarehouseScope ?? true;
        _scopedWarehouseIds = currentUser?.AssignedWarehouseIds.ToArray() ?? [];
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        // Outbox/Inbox/DLQ/audit — satu transaksi dengan state modul
        modelBuilder.AddInfrastructureTables();

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OutboundDbContext).Assembly);

        // Filter akses warehouse berdasarkan current user.
        modelBuilder.Entity<Wave>().HasQueryFilter(
            wave => _bypassWarehouseScope || _scopedWarehouseIds.Contains(wave.WarehouseId));
    }
}
