using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Inventory.Application.ReadModels;

namespace Wms.Inventory.Application.Abstractions;

// Read port tugas putaway — AsNoTracking.
public interface IPutawayTaskReader : IReader
{
    // Antrean tugas Assigned, opsional filter operator.
    Task<IReadOnlyList<PutawayTaskDto>> GetQueueAsync(Guid? assignedTo, CancellationToken cancellationToken = default);

    Task<PutawayTaskDto?> GetByIdAsync(Guid putawayTaskId, CancellationToken cancellationToken = default);
}
