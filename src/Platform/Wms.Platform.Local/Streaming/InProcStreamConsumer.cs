using System.Text.Json;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Streaming;

// Dev — consumer batch-pull. (real: consumer-group Event Hubs / subscription Pub-Sub di fase cloud).
public sealed class InProcStreamConsumer(InProcStreamRing ring) : IEventStreamConsumer
{
    public async Task ConsumeAsync<TEvent>(
        string streamName,
        Func<TEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TEvent : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamName);
        ArgumentNullException.ThrowIfNull(handler);

        while (!cancellationToken.IsCancellationRequested && ring.TryDequeue(streamName, out var payloadJson))
        {
            var payload = JsonSerializer.Deserialize<TEvent>(payloadJson!);
            if (payload is not null)
            {
                await handler(payload, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
