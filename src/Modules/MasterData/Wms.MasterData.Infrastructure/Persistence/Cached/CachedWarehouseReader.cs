using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.ReadModels;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Application.ReadModels;

namespace Wms.MasterData.Infrastructure.Persistence.Cached;

// Cache aside Decorator read port Warehouse — hanya GetById di cache.
internal sealed class CachedWarehouseReader(IWarehouseReader inner, ICacheStore cache) : IWarehouseReader
{
    private static readonly TimeSpan _ttl = TimeSpan.FromMinutes(5);

    public async Task<WarehouseDto?> GetByIdAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var key = $"masterdata:warehouse:{warehouseId}";

        var cached = await cache.GetAsync<WarehouseDto>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var fresh = await inner.GetByIdAsync(warehouseId, cancellationToken);
        if (fresh is not null)
        {
            await cache.SetAsync(key, fresh, _ttl, cancellationToken);
        }

        return fresh;
    }

    public Task<PagedResult<WarehouseDto>> ListAsync(
        int page,
        int pageSize,
        bool includeInactive = false,
        CancellationToken cancellationToken = default) =>
        inner.ListAsync(page, pageSize, includeInactive, cancellationToken);
}
