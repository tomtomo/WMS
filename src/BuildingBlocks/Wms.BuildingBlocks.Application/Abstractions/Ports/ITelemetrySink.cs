namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Sink telemetri diagnostik: OTLP ke Aspire Local, App Insights Azure, Cloud Trace GCP. Beda dari
// IEventStreamPublisher yang membawa event bisnis high-throughput.
public interface ITelemetrySink
{
    Task RecordAsync(
        string name,
        IReadOnlyDictionary<string, string> tags,
        CancellationToken cancellationToken = default);
}
