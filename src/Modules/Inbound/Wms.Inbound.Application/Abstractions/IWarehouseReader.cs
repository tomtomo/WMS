using Wms.BuildingBlocks.Application.ReadModels;

namespace Wms.Inbound.Application.Abstractions;

// Port lookup Warehouse milik Inbound
public interface IWarehouseReader : IReader
{
    Task<bool> ExistsAsync(Guid warehouseId, CancellationToken cancellationToken = default);
}
