using Wms.BuildingBlocks.Application.ReadModels;
using Wms.MasterData.Application.ReadModels;

namespace Wms.MasterData.Application.Abstractions;

// Read port Warehouse
public interface IWarehouseReader : IReader
{
    Task<WarehouseDto?> GetByIdAsync(Guid warehouseId, CancellationToken cancellationToken = default);

    Task<PagedResult<WarehouseDto>> ListAsync(
        int page,
        int pageSize,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
}
