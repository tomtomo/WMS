using Wms.MasterData.Domain;

namespace Wms.MasterData.Application.Abstractions;

// Write side Warehouse. commit oleh TransactionBehavior via IUnitOfWork.
public interface IWarehouseRepository
{
    Task AddAsync(Warehouse warehouse, CancellationToken cancellationToken = default);

    Task<Warehouse?> GetAsync(WarehouseId id, CancellationToken cancellationToken = default);
}
