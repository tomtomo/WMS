using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Wms.Platform.Azure.Messaging;

namespace Wms.Platform.Azure.Eventing;

// Dispatcher tetap sama, tapi transport dipilih dari delivery class.
// Error dari transport sengaja dibiarkan naik agar worker outbox yang menangani retry dan dead letter.
public sealed class AzureOutboxDispatcher(
    ServiceBusMessagePublisher coreFlowRail,
    EventGridNotificationPublisher notificationRail) : OutboxDispatcher
{
    protected override async Task<Result> PublishToCoreFlowAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        await coreFlowRail.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }

    protected override async Task<Result> PublishToNotificationAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        await notificationRail.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
