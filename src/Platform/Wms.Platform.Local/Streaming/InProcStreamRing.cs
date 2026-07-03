using System.Collections.Concurrent;

namespace Wms.Platform.Local.Streaming;

// Dev - in proc bounded per stream. Real adapter = Event Hubs (Azure) / Pub-Sub ke BigQuery (GCP) di cloud.
public sealed class InProcStreamRing
{
    private const int CapacityPerStream = 1024;

    private readonly ConcurrentDictionary<string, ConcurrentQueue<string>> _streams = new(StringComparer.Ordinal);

    public void Enqueue(string streamName, string payloadJson)
    {
        var queue = _streams.GetOrAdd(streamName, _ => new ConcurrentQueue<string>());
        queue.Enqueue(payloadJson);
        while (queue.Count > CapacityPerStream && queue.TryDequeue(out _))
        {
            // Drop oldest: kontrak stream toleran lossy, bukan at least once durable.
        }
    }

    public bool TryDequeue(string streamName, out string? payloadJson)
    {
        payloadJson = null;
        return _streams.TryGetValue(streamName, out var queue)
            && queue.TryDequeue(out payloadJson);
    }
}
