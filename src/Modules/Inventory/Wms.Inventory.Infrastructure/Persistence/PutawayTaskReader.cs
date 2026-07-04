using Microsoft.EntityFrameworkCore;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Application.ReadModels;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.Enums;

namespace Wms.Inventory.Infrastructure.Persistence;

// Read port tugas putaway — AsNoTracking.
internal sealed class PutawayTaskReader(InventoryDbContext context) : IPutawayTaskReader
{
    public async Task<IReadOnlyList<PutawayTaskDto>> GetQueueAsync(
        Guid? assignedTo,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<PutawayTask>().AsNoTracking()
            .Where(task => task.Status == PutawayStatus.Assigned);

        if (assignedTo is not null)
        {
            query = query.Where(task => task.AssignedTo == assignedTo.Value);
        }

        var tasks = await query.ToListAsync(cancellationToken);
        return [.. tasks.Select(Map)];
    }

    public async Task<PutawayTaskDto?> GetByIdAsync(Guid putawayTaskId, CancellationToken cancellationToken = default)
    {
        var id = PutawayTaskId.Create(putawayTaskId);
        if (id.IsFailure)
        {
            return null;
        }

        var task = await context.Set<PutawayTask>().AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id.Value, cancellationToken);
        return task is null ? null : Map(task);
    }

    private static PutawayTaskDto Map(PutawayTask task) => new(
        task.Id.Value,
        task.StockId.Value,
        task.SourceLocationId.Value,
        task.SuggestedDestinationId.Value,
        task.AssignedTo,
        task.Status.ToString(),
        task.ActualDestinationId?.Value);
}
