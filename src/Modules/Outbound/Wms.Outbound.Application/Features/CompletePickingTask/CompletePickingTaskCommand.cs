using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Outbound.Application.Features.CompletePickingTask;

// operator menyelesaikan picking (task Assigned jadi Completed, stock pindah ke staging). Emit PickingCompleted,
// dan saat semua task wave selesai, wave jadi Ready dan emit WaveReady.
[RequiresPermission(OutboundPermissions.CompletePickingTask)]
public sealed record CompletePickingTaskCommand(
    Guid TaskId,
    decimal ActualQty,
    Guid StagingLocationId,
    Guid? OperatorId) : ICommand;
