using Hangfire;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Scheduling;

// Hangfire scheduled job (cloud: Service Bus scheduled / Cloud Tasks)
public sealed class HangfireDelayedTaskQueue(IBackgroundJobClient backgroundJobClient) : IDelayedTaskQueue
{
    public Task<string> ScheduleAsync<TPayload>(
        TPayload payload,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default)
        where TPayload : notnull
    {
        ArgumentNullException.ThrowIfNull(payload);

        var taskId = backgroundJobClient.Schedule<DelayedTaskExecutor<TPayload>>(
            executor => executor.ExecuteAsync(payload, CancellationToken.None),
            dueAt);
        return Task.FromResult(taskId);
    }

    public Task CancelAsync(string taskId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(taskId);

        // Delete idempoten: task yang sudah jalan/tidak ada mengembalikan false.
        backgroundJobClient.Delete(taskId);
        return Task.CompletedTask;
    }
}

// Hangfire mengaktivasi kelas ini via DI lalu meneruskan payload ke handler port.
public sealed class DelayedTaskExecutor<TPayload>(IDelayedTaskHandler<TPayload> handler)
    where TPayload : notnull
{
    public Task ExecuteAsync(TPayload payload, CancellationToken cancellationToken) =>
        handler.HandleAsync(payload, cancellationToken);
}
