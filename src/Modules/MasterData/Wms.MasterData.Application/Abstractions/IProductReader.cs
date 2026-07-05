using Wms.BuildingBlocks.Application.ReadModels;
using Wms.MasterData.Application.ReadModels;

namespace Wms.MasterData.Application.Abstractions;

// Read port Product
public interface IProductReader : IReader
{
    Task<ProductSnapshotDto?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);

    Task<PagedResult<ProductSnapshotDto>> ListAsync(
        int page,
        int pageSize,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
}
