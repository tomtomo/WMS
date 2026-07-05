using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.ReadModels;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Application.ReadModels;
using Wms.MasterData.Domain;

namespace Wms.MasterData.Infrastructure.Persistence;

// Read port EF Warehouse — AsNoTracking.
internal sealed class WarehouseReader(MasterDataDbContext context) : IWarehouseReader
{
    public async Task<WarehouseDto?> GetByIdAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var id = WarehouseId.Create(warehouseId);
        if (id.IsFailure)
        {
            return null;
        }

        var warehouse = await context.Set<Warehouse>().AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id.Value, cancellationToken);
        return warehouse is null ? null : Map(warehouse);
    }

    public async Task<PagedResult<WarehouseDto>> ListAsync(
        int page,
        int pageSize,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var currentPage = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 200);

        IQueryable<Warehouse> query = context.Set<Warehouse>().AsNoTracking();
        if (includeInactive)
        {
            // tampilkan yang soft deleted
            query = query.IgnoreQueryFilters();
        }

        var total = await query.CountAsync(cancellationToken);
        var warehouses = await query
            .OrderBy(warehouse => warehouse.Name)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new PagedResult<WarehouseDto>([.. warehouses.Select(Map)], total, currentPage, size);
    }

    private static WarehouseDto Map(Warehouse warehouse) =>
        new(warehouse.Id.Value, warehouse.Name, warehouse.Address, warehouse.IsActive);
}
