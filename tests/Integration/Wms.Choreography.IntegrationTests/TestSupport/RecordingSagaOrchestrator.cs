using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Choreography.IntegrationTests.TestSupport;

// membuktikan auto cancel Unfulfilled
internal sealed class RecordingSagaOrchestrator : ISagaOrchestrator
{
    private int _started;

    public int Started => Volatile.Read(ref _started);

    public Task StartAsync<TSagaData>(string sagaId, TSagaData data, CancellationToken cancellationToken = default)
        where TSagaData : notnull
    {
        Interlocked.Increment(ref _started);
        return Task.CompletedTask;
    }
}
