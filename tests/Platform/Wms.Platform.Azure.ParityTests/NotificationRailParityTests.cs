using AwesomeAssertions;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Contracts.Abstractions;
using Wms.Platform.Azure.Messaging;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Rail notification memakai CloudEvent: metadata disimpan sebagai atribut, payload tetap menjadi data utama.
public sealed class NotificationRailParityTests
{
    private static readonly MessageEnvelope _envelope = new(
        Guid.Parse("44444444-4444-4444-4444-444444444444"),
        "inbound.goods_receipt_pending_review.v1",
        DeliveryClass.Notification,
        new DateTimeOffset(2026, 7, 10, 2, 3, 4, TimeSpan.Zero),
        """{"grId":"abc","hasOverDelivery":true}""",
        "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
        "congo=t61rcWkgMzE",
        null);

    [Fact]
    public void Cloud_event_carries_envelope_metadata_as_attributes()
    {
        var cloudEvent = EventGridEnvelopeMapper.ToCloudEvent(_envelope);

        cloudEvent.Id.Should().Be(_envelope.EventId.ToString(), "id CE = eventId, kunci dedup Inbox");
        cloudEvent.Type.Should().Be(_envelope.LogicalName);
        cloudEvent.Time.Should().Be(_envelope.OccurredAt);
        cloudEvent.Data!.ToString().Should().Be(_envelope.Payload, "data CE = payload murni, tanpa nested envelope");
        cloudEvent.ExtensionAttributes["deliveryclass"].Should().Be("Notification");
        cloudEvent.ExtensionAttributes["traceparent"].Should().Be(_envelope.Traceparent);
        cloudEvent.ExtensionAttributes["tracestate"].Should().Be(_envelope.Tracestate);
    }

    [Fact]
    public void Cloud_event_round_trip_is_lossless()
    {
        var restored = EventGridEnvelopeMapper.ToEnvelope(EventGridEnvelopeMapper.ToCloudEvent(_envelope));

        restored.Should().Be(_envelope);
    }

    [Fact]
    public void Cloud_event_round_trip_preserves_partition_key_of_a_dual_class_event()
    {
        var dualClassNotification = _envelope with
        {
            LogicalName = "inventory.stock_allocation_completed.v1",
            PartitionKey = "77777777-7777-7777-7777-777777777777",
        };

        var cloudEvent = EventGridEnvelopeMapper.ToCloudEvent(dualClassNotification);
        var restored = EventGridEnvelopeMapper.ToEnvelope(cloudEvent);

        cloudEvent.ExtensionAttributes["partitionkey"].Should().Be("77777777-7777-7777-7777-777777777777");
        restored.Should().Be(dualClassNotification);
    }

    [Fact]
    public void Null_optionals_are_not_written_as_attributes()
    {
        var bare = _envelope with { Traceparent = null, Tracestate = null };

        var cloudEvent = EventGridEnvelopeMapper.ToCloudEvent(bare);

        cloudEvent.ExtensionAttributes.Keys.Should().NotContain(["traceparent", "tracestate", "partitionkey"]);
        EventGridEnvelopeMapper.ToEnvelope(cloudEvent).Should().Be(bare);
    }
}
