using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.BuildingBlocks.Application.Abstractions.Ports;

// Port untuk menerima message envelope dari transport.
public interface IMessageSubscriber
{
    Task SubscribeAsync(
        string queueName,
        IReadOnlyCollection<RailSubscription> subscriptions,
        Func<MessageEnvelope, CancellationToken, Task<bool>> onMessageAsync,
        CancellationToken cancellationToken = default);
}
