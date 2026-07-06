using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Reporting.Abstractions;
using Wms.Reporting.Persistence;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Queries;

// Read port Operator Productivity — AsNoTracking.
internal sealed class OperatorActivityReader(ReportingDbContext context) : IOperatorActivityReader
{
    public async Task<PagedResult<OperatorProductivityRow>> ListAsync(
        Guid? operatorId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var currentPage = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 200);

        IQueryable<OperatorActivity> query = context.Set<OperatorActivity>().AsNoTracking();
        if (operatorId is { } operatorFilter)
        {
            query = query.Where(activity => activity.OperatorId == operatorFilter);
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(activity => activity.OperatorId).ThenBy(activity => activity.Period)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new PagedResult<OperatorProductivityRow>([.. rows.Select(Map)], total, currentPage, size);
    }

    private static OperatorProductivityRow Map(OperatorActivity activity) =>
        new(activity.OperatorId, activity.Period, activity.PutawayCount, activity.PickCount);
}
