namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Publish event bisnis bervolume tinggi ke stream dengan at least once, replayable, toleran-lossy (Event Hubs Azure, Pub/Sub ke BigQuery GCP, stub Local).
public interface IEventStreamPublisher
{
    Task PublishAsync<TEvent>(string streamName, TEvent payload, CancellationToken cancellationToken = default)
        where TEvent : notnull;
}
