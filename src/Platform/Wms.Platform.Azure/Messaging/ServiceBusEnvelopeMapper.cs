using System.Globalization;
using Azure.Messaging.ServiceBus;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Contracts.Abstractions;

namespace Wms.Platform.Azure.Messaging;

// Mapper antara MessageEnvelope dan Message Service Bus.
public static class ServiceBusEnvelopeMapper
{
    private const string DeliveryClassProperty = "deliveryClass";
    private const string OccurredAtProperty = "occurredAt";
    private const string TraceparentProperty = "traceparent";
    private const string TracestateProperty = "tracestate";
    private const string PartitionKeyProperty = "partitionKey";

    public static ServiceBusMessage ToServiceBusMessage(MessageEnvelope envelope)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var message = new ServiceBusMessage(envelope.Payload)
        {
            MessageId = envelope.EventId.ToString(),
            Subject = envelope.LogicalName,
            ContentType = "application/json",

            // Entity dengan session butuh SessionId di setiap message. Kalau tidak ada partition key, event type dipakai sebagai fallback.
            SessionId = envelope.PartitionKey ?? envelope.LogicalName,
        };

        message.ApplicationProperties[DeliveryClassProperty] = envelope.DeliveryClass.ToString();

        // Simpan timestamp sebagai string ISO-8601 agar offset dan presisi tetap utuh.
        message.ApplicationProperties[OccurredAtProperty] = envelope.OccurredAt.ToString("O", CultureInfo.InvariantCulture);

        if (envelope.Traceparent is not null)
        {
            message.ApplicationProperties[TraceparentProperty] = envelope.Traceparent;
        }

        if (envelope.Tracestate is not null)
        {
            message.ApplicationProperties[TracestateProperty] = envelope.Tracestate;
        }

        if (envelope.PartitionKey is not null)
        {
            message.ApplicationProperties[PartitionKeyProperty] = envelope.PartitionKey;
        }

        return message;
    }

    // Message yang formatnya salah akan gagal, lalu receiver membiarkannya masuk ke DLQ setelah batas retry.
    public static MessageEnvelope ToEnvelope(ServiceBusReceivedMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        return new MessageEnvelope(
            Guid.Parse(message.MessageId),
            message.Subject,
            Enum.Parse<DeliveryClass>((string)message.ApplicationProperties[DeliveryClassProperty]),
            DateTimeOffset.ParseExact(
                (string)message.ApplicationProperties[OccurredAtProperty], "O", CultureInfo.InvariantCulture),
            message.Body.ToString(),
            OptionalProperty(message, TraceparentProperty),
            OptionalProperty(message, TracestateProperty),
            OptionalProperty(message, PartitionKeyProperty));
    }

    private static string? OptionalProperty(ServiceBusReceivedMessage message, string name) =>
        message.ApplicationProperties.TryGetValue(name, out var value) ? (string?)value : null;
}
