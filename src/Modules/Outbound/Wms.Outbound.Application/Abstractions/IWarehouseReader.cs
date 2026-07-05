using Wms.BuildingBlocks.Application.ReadModels;

namespace Wms.Outbound.Application.Abstractions;

// Port lookup Warehouse milik Outbound
public interface IWarehouseReader : IReader
{
    Task<bool> ExistsAsync(Guid warehouseId, CancellationToken cancellationToken = default);
}
