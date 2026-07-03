using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Port — message publisher
public interface IMessagePublisher
{
    Task PublishAsync<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        DeliveryClass deliveryClass,
        CancellationToken cancellationToken = default)
        where TIntegrationEvent : IIntegrationEvent;
}
