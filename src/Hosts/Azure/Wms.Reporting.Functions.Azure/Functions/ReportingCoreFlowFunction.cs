using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Wms.BuildingBlocks.Infrastructure.Eventing;
using Wms.Platform.Azure.Messaging;

namespace Wms.Reporting.Functions.Azure;

// Trigger ini menerima event core flow dari Service Bus dan meneruskannya ke rail dispatcher.
// Jika proses gagal, runtime akan mencoba ulang sampai pesan masuk dead letter.
public sealed class ReportingCoreFlowFunction(RailConsumerDispatcher dispatcher)
{
    // Nama topic dan subscription harus sama dengan resource yang diprovisi oleh Bicep.
    private const string CoreFlowTopicName = "wms-core-flow";
    private const string SubscriptionName = "wms.reporting";

    [Function("ReportingCoreFlow")]
    public async Task RunAsync(
        [ServiceBusTrigger(CoreFlowTopicName, SubscriptionName, Connection = "ServiceBusConnection", IsSessionsEnabled = true)]
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        var envelope = ServiceBusEnvelopeMapper.ToEnvelope(message);
        await dispatcher.DispatchAsync(envelope, cancellationToken);
    }
}
