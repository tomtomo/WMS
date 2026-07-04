using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.BuildingBlocks.Infrastructure.Messaging;

// Envelope berversi: metadata teknis dibungkus di luar payload domain murni.
// Payload = JSON event POCO. Transport tak perlu inspect tipe.
public sealed partial record MessageEnvelope(
    Guid EventId,
    string LogicalName,
    DeliveryClass DeliveryClass,
    DateTimeOffset OccurredAt,
    string Payload,
    string? Traceparent,
    string? Tracestate)
{
    public static JsonSerializerOptions PayloadSerializerOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static bool IsValidLogicalName(string? logicalName) =>
        logicalName is not null && LogicalNameRegex().IsMatch(logicalName);

    // Bungkus event jadi envelope: payload diserialize dari tipe runtime.
    public static MessageEnvelope Create<TIntegrationEvent>(
        TIntegrationEvent integrationEvent,
        string logicalName,
        DeliveryClass deliveryClass,
        Guid eventId,
        DateTimeOffset occurredAt)
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        if (!IsValidLogicalName(logicalName))
        {
            throw new ArgumentException(
                $"LogicalName '{logicalName}' tidak sesuai format {{module}}.{{event}}.v{{N}}.",
                nameof(logicalName));
        }

        var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), PayloadSerializerOptions);
        return new MessageEnvelope(eventId, logicalName, deliveryClass, occurredAt, payload, null, null);
    }

    // Broker identity {module}.{event}.v{N}
    [GeneratedRegex(@"^[a-z]+\.[a-z0-9_]+\.v\d+$")]
    private static partial Regex LogicalNameRegex();
}
