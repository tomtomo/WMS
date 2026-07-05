using Microsoft.EntityFrameworkCore;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Application.ReadModels;
using Wms.Outbound.Domain;
using Wms.Outbound.Domain.Enums;

namespace Wms.Outbound.Infrastructure.Persistence;

// Read port worklist PickingTask — AsNoTracking, map ke DTO.
internal sealed class PickingTaskReader(OutboundDbContext context) : IPickingTaskReader
{
    public async Task<IReadOnlyList<PickingTaskDto>> GetWorklistAsync(
        Guid? assignedTo,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<PickingTask>().AsNoTracking()
            .Where(task => task.Status == PickingTaskStatus.Assigned);

        if (assignedTo is not null)
        {
            query = query.Where(task => task.AssignedTo == assignedTo.Value);
        }

        var tasks = await query.ToListAsync(cancellationToken);
        return [.. tasks.Select(Map)];
    }

    public async Task<PickingTaskDto?> GetByIdAsync(Guid pickingTaskId, CancellationToken cancellationToken = default)
    {
        var id = PickingTaskId.Create(pickingTaskId);
        if (id.IsFailure)
        {
            return null;
        }

        var task = await context.Set<PickingTask>().AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id.Value, cancellationToken);
        return task is null ? null : Map(task);
    }

    private static PickingTaskDto Map(PickingTask task) => new(
        task.Id.Value,
        task.WaveId.Value,
        task.StockId,
        task.SourceLocationId,
        task.Sku,
        task.Batch,
        task.Qty,
        task.AssignedTo,
        task.Status.ToString(),
        task.ActualQty,
        task.StagingLocationId);
}
