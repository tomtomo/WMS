using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.ReadModels;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Application.ReadModels;

namespace Wms.MasterData.Infrastructure.Persistence.Cached;

// GoF Decorator (Open/Closed) cache aside di atas read port Product.
internal sealed class CachedProductReader(IProductReader inner, ICacheStore cache) : IProductReader
{
    private static readonly TimeSpan _ttl = TimeSpan.FromMinutes(5);

    public async Task<ProductSnapshotDto?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var key = $"masterdata:product:{sku}";

        var cached = await cache.GetAsync<ProductSnapshotDto>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var fresh = await inner.GetBySkuAsync(sku, cancellationToken);
        if (fresh is not null)
        {
            // Hanya non null dicache. not found selalu fall through ke inner reader.
            await cache.SetAsync(key, fresh, _ttl, cancellationToken);
        }

        return fresh;
    }

    public Task<PagedResult<ProductSnapshotDto>> ListAsync(
        int page,
        int pageSize,
        bool includeInactive = false,
        CancellationToken cancellationToken = default) =>
        inner.ListAsync(page, pageSize, includeInactive, cancellationToken);
}
