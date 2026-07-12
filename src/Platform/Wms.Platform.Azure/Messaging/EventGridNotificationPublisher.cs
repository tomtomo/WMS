using Azure.Messaging.EventGrid;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Contracts.Abstractions;

namespace Wms.Platform.Azure.Messaging;

// Publisher untuk notification lewat Event Grid. Cocok untuk notifikasi ke banyak subscriber, tanpa jaminan urutan antar event.
// Bukan implement IMessagePublisher
public sealed class EventGridNotificationPublisher(EventGridPublisherClient client)
{
    public async Task PublishAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        if (envelope.DeliveryClass != DeliveryClass.Notification)
        {
            throw new InvalidOperationException(
                $"Salah rail: '{envelope.LogicalName}' ({envelope.DeliveryClass}) bukan Notification");
        }

        await client
            .SendEventAsync(EventGridEnvelopeMapper.ToCloudEvent(envelope), cancellationToken)
            .ConfigureAwait(false);
    }
}
