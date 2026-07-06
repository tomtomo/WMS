using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Infrastructure.Outbox;

namespace Wms.Platform.Local.Eventing;

// Dispatcher outbox berbasis RabbitMQ.
public sealed class RabbitMqOutboxDispatcher(IMessagePublisher publisher) : OutboxDispatcher
{
    protected override Task<Result> PublishToCoreFlowAsync(MessageEnvelope envelope, CancellationToken cancellationToken) =>
        PublishAsync(envelope, cancellationToken);

    protected override Task<Result> PublishToNotificationAsync(MessageEnvelope envelope, CancellationToken cancellationToken) =>
        PublishAsync(envelope, cancellationToken);

    private async Task<Result> PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
