using System.Collections.Concurrent;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Inventory.IntegrationTests.TestSupport;

// Test double: rekam event yang dikirim ke stream agar pengiriman telemetry bisa diverifikasi dalam test.
internal sealed class CapturingEventStreamPublisher : IEventStreamPublisher
{
    private readonly ConcurrentQueue<(string Stream, object Payload)> _published = new();

    public Task PublishAsync<TEvent>(string streamName, TEvent payload, CancellationToken cancellationToken = default)
        where TEvent : notnull
    {
        _published.Enqueue((streamName, payload));
        return Task.CompletedTask;
    }

    public IReadOnlyList<TEvent> On<TEvent>(string streamName) =>
        [.. _published.Where(entry => entry.Stream == streamName).Select(entry => entry.Payload).OfType<TEvent>()];
}
