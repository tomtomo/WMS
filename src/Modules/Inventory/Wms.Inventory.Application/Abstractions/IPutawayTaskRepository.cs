using Wms.Inventory.Domain;

namespace Wms.Inventory.Application.Abstractions;

// Write side PutawayTask.
public interface IPutawayTaskRepository
{
    Task AddAsync(PutawayTask putawayTask, CancellationToken cancellationToken = default);

    Task<PutawayTask?> GetAsync(PutawayTaskId id, CancellationToken cancellationToken = default);
}
