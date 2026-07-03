namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Scheduler satu eksekusi yang durable dan survive restart: Hangfire Local, Service Bus scheduled Azure, Cloud Tasks GCP.
public interface IDelayedTaskQueue
{
    Task<string> ScheduleAsync<TPayload>(
        TPayload payload,
        DateTimeOffset dueAt,
        CancellationToken cancellationToken = default)
        where TPayload : notnull;

    Task CancelAsync(string taskId, CancellationToken cancellationToken = default);
}

// Handler untuk payload delayed task, diresolve adapter berdasar tipe payload.
public interface IDelayedTaskHandler<in TPayload>
    where TPayload : notnull
{
    Task HandleAsync(TPayload payload, CancellationToken cancellationToken = default);
}
