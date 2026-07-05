using Microsoft.EntityFrameworkCore;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Domain;

namespace Wms.MasterData.Infrastructure.Persistence;

// Write side Location (tracked), dicommit oleh TransactionBehavior via IUnitOfWork.
internal sealed class LocationRepository(MasterDataDbContext context) : ILocationRepository
{
    public Task AddAsync(Location location, CancellationToken cancellationToken = default)
    {
        context.Set<Location>().Add(location);
        return Task.CompletedTask;
    }

    // load termasuk yang soft deleted
    public Task<Location?> GetAsync(LocationId id, CancellationToken cancellationToken = default) =>
        context.Set<Location>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(location => location.Id == id, cancellationToken);

    // hanya Warehouse aktif yang boleh ditambah Location
    public Task<bool> WarehouseExistsAsync(WarehouseId warehouseId, CancellationToken cancellationToken = default) =>
        context.Set<Warehouse>().AnyAsync(warehouse => warehouse.Id == warehouseId, cancellationToken);
}
