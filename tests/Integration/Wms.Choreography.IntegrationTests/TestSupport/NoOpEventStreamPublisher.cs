using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Choreography.IntegrationTests.TestSupport;

// Choreography test alur domain, bukan telemetry
internal sealed class NoOpEventStreamPublisher : IEventStreamPublisher
{
    public Task PublishAsync<TEvent>(string streamName, TEvent payload, CancellationToken cancellationToken = default)
        where TEvent : notnull => Task.CompletedTask;
}
