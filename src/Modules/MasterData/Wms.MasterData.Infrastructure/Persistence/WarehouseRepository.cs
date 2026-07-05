using Microsoft.EntityFrameworkCore;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Domain;

namespace Wms.MasterData.Infrastructure.Persistence;

// Write side Warehouse (tracked). commit oleh TransactionBehavior via IUnitOfWork.
internal sealed class WarehouseRepository(MasterDataDbContext context) : IWarehouseRepository
{
    public Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
    {
        context.Set<Warehouse>().Add(warehouse);
        return Task.CompletedTask;
    }

    // termasuk yang soft deleted
    public Task<Warehouse?> GetAsync(WarehouseId id, CancellationToken cancellationToken = default) =>
        context.Set<Warehouse>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(warehouse => warehouse.Id == id, cancellationToken);
}
