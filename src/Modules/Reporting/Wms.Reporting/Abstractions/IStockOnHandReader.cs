using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Abstractions;

// Read port Stock on Hand
public interface IStockOnHandReader : IReader
{
    Task<PagedResult<StockOnHandRow>> ListAsync(
        Guid? warehouseId,
        string? sku,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
