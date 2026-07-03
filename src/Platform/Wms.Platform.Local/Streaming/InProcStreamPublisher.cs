using System.Text.Json;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Streaming;

// Dev — publisher stream in-proc.
public sealed class InProcStreamPublisher(InProcStreamRing ring) : IEventStreamPublisher
{
    public Task PublishAsync<TEvent>(string streamName, TEvent payload, CancellationToken cancellationToken = default)
        where TEvent : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamName);
        ArgumentNullException.ThrowIfNull(payload);

        ring.Enqueue(streamName, JsonSerializer.Serialize(payload, payload.GetType()));
        return Task.CompletedTask;
    }
}
