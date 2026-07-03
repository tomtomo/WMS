using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Saga;

// Saga in-proc (cloud: Durable Functions / Workflows) dengan state machine minimal Started. Choreography tetap
// default flow, orchestrator hanya untuk multi step berkompensasi.
public sealed class InProcSagaOrchestrator(ILogger<InProcSagaOrchestrator> logger) : ISagaOrchestrator
{
    public const string StartedStatus = "Started";

    private readonly ConcurrentDictionary<string, SagaInstance> _instances = new(StringComparer.Ordinal);

    public Task StartAsync<TSagaData>(string sagaId, TSagaData data, CancellationToken cancellationToken = default)
        where TSagaData : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sagaId);
        ArgumentNullException.ThrowIfNull(data);

        if (!_instances.TryAdd(sagaId, new SagaInstance(StartedStatus, data)))
        {
            throw new InvalidOperationException($"Saga '{sagaId}' sudah dimulai — start ganda ditolak.");
        }

        logger.LogInformation("Saga {SagaId} transisi ke {SagaStatus}", sagaId, StartedStatus);
        return Task.CompletedTask;
    }

    public bool TryGetStatus(string sagaId, out string? status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sagaId);

        if (_instances.TryGetValue(sagaId, out var instance))
        {
            status = instance.Status;
            return true;
        }

        status = null;
        return false;
    }

    private sealed record SagaInstance(string Status, object Data);
}
