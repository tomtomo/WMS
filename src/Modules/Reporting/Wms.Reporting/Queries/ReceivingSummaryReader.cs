using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Reporting.Abstractions;
using Wms.Reporting.Persistence;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Queries;

// Read port Supplier Performance.
internal sealed class ReceivingSummaryReader(ReportingDbContext context) : IReceivingSummaryReader
{
    public async Task<PagedResult<SupplierPerformanceRow>> ListAsync(
        Guid? supplierId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var currentPage = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 200);

        IQueryable<ReceivingSummary> query = context.Set<ReceivingSummary>().AsNoTracking();
        if (supplierId is { } supplier)
        {
            query = query.Where(summary => summary.SupplierId == supplier);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(summary => summary.SupplierId).ThenBy(summary => summary.Period)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new PagedResult<SupplierPerformanceRow>([.. rows.Select(Map)], total, currentPage, size);
    }

    private static SupplierPerformanceRow Map(ReceivingSummary summary) => new(
        summary.SupplierId,
        summary.Period,
        summary.ReceivedQty,
        summary.ReceiptCount,
        summary.DiscrepancyCount,
        summary.ReceiptCount == 0 ? 0m : decimal.Round((decimal)summary.DiscrepancyCount / summary.ReceiptCount, 4));
}
