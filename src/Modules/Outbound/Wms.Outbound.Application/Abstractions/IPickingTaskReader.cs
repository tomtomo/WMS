using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Outbound.Application.ReadModels;

namespace Wms.Outbound.Application.Abstractions;

// Read port worklist PickingTask per operator.
public interface IPickingTaskReader : IReader
{
    // Worklist task Assigned, opsional filter operator.
    Task<IReadOnlyList<PickingTaskDto>> GetWorklistAsync(
        Guid? assignedTo,
        CancellationToken cancellationToken = default);

    Task<PickingTaskDto?> GetByIdAsync(Guid pickingTaskId, CancellationToken cancellationToken = default);
}
