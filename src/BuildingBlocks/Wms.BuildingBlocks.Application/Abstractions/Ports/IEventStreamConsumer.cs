namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Port Consumer stream high-throughput(Event Hubs, Pub/Sub). Pasangan IEventStreamPublisher.
public interface IEventStreamConsumer
{
    Task ConsumeAsync<TEvent>(
        string streamName,
        Func<TEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TEvent : notnull;
}
