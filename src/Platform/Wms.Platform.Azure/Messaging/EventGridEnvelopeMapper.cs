using Azure.Messaging;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Contracts.Abstractions;

namespace Wms.Platform.Azure.Messaging;

// Mapper antara MessageEnvelope dan CloudEvent untuk rail notification.
// Metadata disimpan sebagai atribut CloudEvent, sedangkan data tetap payload asli.
public static class EventGridEnvelopeMapper
{
    // Nama atribut ekstensi CloudEvent harus lowercase alfanumerik.
    // traceparent dan tracestate mengikuti format distributed tracing CloudEvents.
    private const string DeliveryClassAttribute = "deliveryclass";
    private const string TraceparentAttribute = "traceparent";
    private const string TracestateAttribute = "tracestate";
    private const string PartitionKeyAttribute = "partitionkey";
    private const string Source = "/wms/outbox";

    public static CloudEvent ToCloudEvent(MessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var cloudEvent = new CloudEvent(
            Source,
            envelope.LogicalName,
            BinaryData.FromString(envelope.Payload),
            "application/json",
            CloudEventDataFormat.Json)
        {
            Id = envelope.EventId.ToString(),
            Time = envelope.OccurredAt,
        };

        cloudEvent.ExtensionAttributes[DeliveryClassAttribute] = envelope.DeliveryClass.ToString();

        if (envelope.Traceparent is not null)
        {
            cloudEvent.ExtensionAttributes[TraceparentAttribute] = envelope.Traceparent;
        }

        if (envelope.Tracestate is not null)
        {
            cloudEvent.ExtensionAttributes[TracestateAttribute] = envelope.Tracestate;
        }

        if (envelope.PartitionKey is not null)
        {
            cloudEvent.ExtensionAttributes[PartitionKeyAttribute] = envelope.PartitionKey;
        }

        return cloudEvent;
    }

    public static MessageEnvelope ToEnvelope(CloudEvent cloudEvent)
    {
        ArgumentNullException.ThrowIfNull(cloudEvent);

        return new MessageEnvelope(
            Guid.Parse(cloudEvent.Id),
            cloudEvent.Type,
            Enum.Parse<DeliveryClass>((string)cloudEvent.ExtensionAttributes[DeliveryClassAttribute]),
            cloudEvent.Time ?? throw new InvalidOperationException("CloudEvent tanpa atribut time."),
            cloudEvent.Data?.ToString() ?? throw new InvalidOperationException("CloudEvent tanpa data payload."),
            OptionalAttribute(cloudEvent, TraceparentAttribute),
            OptionalAttribute(cloudEvent, TracestateAttribute),
            OptionalAttribute(cloudEvent, PartitionKeyAttribute));
    }

    private static string? OptionalAttribute(CloudEvent cloudEvent, string name) =>
        cloudEvent.ExtensionAttributes.TryGetValue(name, out var value) ? (string?)value : null;
}
