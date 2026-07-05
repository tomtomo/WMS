using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.ReadModels;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Application.ReadModels;
using Wms.MasterData.Domain;

namespace Wms.MasterData.Infrastructure.Persistence;

// Read port EF Location — AsNoTracking.
internal sealed class LocationReader(MasterDataDbContext context) : ILocationReader
{
    public async Task<LocationDto?> GetByIdAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        var id = LocationId.Create(locationId);
        if (id.IsFailure)
        {
            return null;
        }

        var location = await context.Set<Location>().AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id.Value, cancellationToken);
        return location is null ? null : Map(location);
    }

    public async Task<PagedResult<LocationDto>> ListAsync(
        int page,
        int pageSize,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var currentPage = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 200);

        IQueryable<Location> query = context.Set<Location>().AsNoTracking();
        if (includeInactive)
        {
            query = query.IgnoreQueryFilters();
        }

        var total = await query.CountAsync(cancellationToken);
        var locations = await query
            .OrderBy(location => location.Code)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new PagedResult<LocationDto>([.. locations.Select(Map)], total, currentPage, size);
    }

    private static LocationDto Map(Location location) =>
        new(location.Id.Value, location.WarehouseId.Value, location.Type.ToString(), location.Code, location.IsActive);
}
