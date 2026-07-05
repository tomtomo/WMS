using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.ReadModels;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Application.ReadModels;

namespace Wms.MasterData.Infrastructure.Persistence.Cached;

// Cache aside Decorator read port Location
internal sealed class CachedLocationReader(ILocationReader inner, ICacheStore cache) : ILocationReader
{
    private static readonly TimeSpan _ttl = TimeSpan.FromMinutes(5);

    public async Task<LocationDto?> GetByIdAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        var key = $"masterdata:location:{locationId}";

        var cached = await cache.GetAsync<LocationDto>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var fresh = await inner.GetByIdAsync(locationId, cancellationToken);
        if (fresh is not null)
        {
            await cache.SetAsync(key, fresh, _ttl, cancellationToken);
        }

        return fresh;
    }

    public Task<PagedResult<LocationDto>> ListAsync(
        int page,
        int pageSize,
        bool includeInactive = false,
        CancellationToken cancellationToken = default) =>
        inner.ListAsync(page, pageSize, includeInactive, cancellationToken);
}
