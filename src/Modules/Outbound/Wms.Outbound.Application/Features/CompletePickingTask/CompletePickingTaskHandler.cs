using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Application.EventTranslation;
using Wms.Outbound.Domain;

namespace Wms.Outbound.Application.Features.CompletePickingTask;

// PickingTask Assigned ke Completed lalu emit PickingCompleted. Jika semua task
// wave Completed lalu WaveReady. Commit oleh TransactionBehavior.
internal sealed class CompletePickingTaskHandler(
    IPickingTaskRepository pickingTaskRepository,
    IWaveRepository waveRepository,
    OutboundEventTranslator translator) : ICommandHandler<CompletePickingTaskCommand>
{
    public async Task<Result> Handle(CompletePickingTaskCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var taskId = PickingTaskId.Create(command.TaskId);
        if (taskId.IsFailure)
        {
            return taskId;
        }

        var task = await pickingTaskRepository.GetAsync(taskId.Value, cancellationToken);
        if (task is null)
        {
            return Result.NotFound(new Error("picking_task.not_found", "PickingTask tidak ditemukan."));
        }

        var completed = task.Complete(command.ActualQty, command.StagingLocationId);
        if (completed.IsFailure)
        {
            return completed;
        }

        await translator.CompletePickingAsync(task, command.OperatorId, cancellationToken);

        var wave = await waveRepository.GetAsync(task.WaveId, cancellationToken);
        if (wave is null)
        {
            return Result.NotFound(new Error("wave.not_found", "Wave PickingTask tidak ditemukan."));
        }

        var tasks = await pickingTaskRepository.ListByWaveAsync(task.WaveId, cancellationToken);
        var readiness = wave.EvaluateReadiness(tasks);
        if (readiness.IsFailure)
        {
            return readiness;
        }

        // Emit WaveReady jika transisi ke Ready terjadi (WaveReadyRaised), lalu clear.
        await translator.TranslateWaveEventsAsync(wave, cancellationToken);
        return Result.Success();
    }
}
