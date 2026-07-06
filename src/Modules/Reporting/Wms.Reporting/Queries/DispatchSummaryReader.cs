using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Reporting.Abstractions;
using Wms.Reporting.Persistence;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Queries;

// Read port Dispatch Summary — AsNoTracking.
internal sealed class DispatchSummaryReader(ReportingDbContext context) : IDispatchSummaryReader
{
    public async Task<PagedResult<DispatchSummaryRow>> ListAsync(
        Guid? warehouseId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var currentPage = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 200);

        IQueryable<DispatchSummary> query = context.Set<DispatchSummary>().AsNoTracking();
        if (warehouseId is { } warehouse)
        {
            query = query.Where(summary => summary.WarehouseId == warehouse);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(summary => summary.WarehouseId).ThenBy(summary => summary.Period)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new PagedResult<DispatchSummaryRow>([.. rows.Select(Map)], total, currentPage, size);
    }

    private static DispatchSummaryRow Map(DispatchSummary summary) =>
        new(summary.WarehouseId, summary.Period, summary.DispatchedVolume, summary.WaveThroughput);
}
