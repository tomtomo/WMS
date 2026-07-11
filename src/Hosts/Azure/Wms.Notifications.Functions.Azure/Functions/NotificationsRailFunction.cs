using Azure.Messaging;
using Microsoft.Azure.Functions.Worker;
using Wms.BuildingBlocks.Infrastructure.Eventing;
using Wms.Platform.Azure.Messaging;

namespace Wms.Notifications.Functions.Azure;

// Trigger ini menerima event notifikasi dari Event Grid dan meneruskannya ke rail dispatcher.
// Exception dibiarkan naik agar Event Grid dapat melakukan retry hingga event masuk dead letter.
public sealed class NotificationsRailFunction(RailConsumerDispatcher dispatcher)
{
    [Function("NotificationsRail")]
    public async Task RunAsync([EventGridTrigger] CloudEvent cloudEvent, CancellationToken cancellationToken)
    {
        var envelope = EventGridEnvelopeMapper.ToEnvelope(cloudEvent);
        await dispatcher.DispatchAsync(envelope, cancellationToken);
    }
}
