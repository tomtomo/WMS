using Wms.MasterData.Domain;

namespace Wms.MasterData.Application.Abstractions;

// Write side Location. commit oleh TransactionBehavior via IUnitOfWork.
public interface ILocationRepository
{
    Task AddAsync(Location location, CancellationToken cancellationToken = default);

    Task<Location?> GetAsync(LocationId id, CancellationToken cancellationToken = default);

    // Validasi FK Warehouse exists sebelum create Location
    Task<bool> WarehouseExistsAsync(WarehouseId warehouseId, CancellationToken cancellationToken = default);
}
