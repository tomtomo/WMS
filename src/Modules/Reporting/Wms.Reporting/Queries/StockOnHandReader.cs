using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Reporting.Abstractions;
using Wms.Reporting.Persistence;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Queries;

// Read port Stock on Hand — AsNoTracking.
internal sealed class StockOnHandReader(ReportingDbContext context) : IStockOnHandReader
{
    public async Task<PagedResult<StockOnHandRow>> ListAsync(
        Guid? warehouseId,
        string? sku,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var currentPage = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 200);

        IQueryable<StockOnHandView> query = context.Set<StockOnHandView>().AsNoTracking();
        if (warehouseId is { } warehouse)
        {
            query = query.Where(view => view.WarehouseId == warehouse);
        }

        if (!string.IsNullOrWhiteSpace(sku))
        {
            query = query.Where(view => view.Sku == sku);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(view => view.WarehouseId).ThenBy(view => view.Sku).ThenBy(view => view.Batch)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new PagedResult<StockOnHandRow>([.. rows.Select(Map)], total, currentPage, size);
    }

    private static StockOnHandRow Map(StockOnHandView view) =>
        new(view.WarehouseId, view.Sku, view.Batch.Length == 0 ? null : view.Batch, view.QtyOnHand);
}
