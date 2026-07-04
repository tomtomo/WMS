using Microsoft.EntityFrameworkCore;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Domain;

namespace Wms.Inventory.Infrastructure.Persistence;

// Write side PutawayTask: tracked, commit oleh TransactionBehavior via IUnitOfWork.
internal sealed class PutawayTaskRepository(InventoryDbContext context) : IPutawayTaskRepository
{
    public Task AddAsync(PutawayTask putawayTask, CancellationToken cancellationToken = default)
    {
        context.Set<PutawayTask>().Add(putawayTask);
        return Task.CompletedTask;
    }

    public Task<PutawayTask?> GetAsync(PutawayTaskId id, CancellationToken cancellationToken = default) =>
        context.Set<PutawayTask>().FirstOrDefaultAsync(task => task.Id == id, cancellationToken);
}
