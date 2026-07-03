using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Port — message subscriber
public interface IMessageSubscriber
{
    Task SubscribeAsync<TIntegrationEvent>(
        Func<TIntegrationEvent, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent;
}
