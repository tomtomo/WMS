using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Contracts.Abstractions;
using Wms.Platform.Azure.Messaging;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Mapper envelope dan Service Bus harus bolak balik tanpa mengubah isi, dan bisa dites tanpa broker.
public sealed class EnvelopeMapperParityTests
{
    private static readonly Guid _eventId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    // Pakai offset non UTC dan pecahan detik untuk memastikan occurredAt tetap utuh
    private static readonly DateTimeOffset _occurredAt =
        new DateTimeOffset(2026, 7, 10, 1, 2, 3, TimeSpan.FromHours(7)).AddTicks(1234567);

    private static MessageEnvelope FullEnvelope => new(
        _eventId,
        "inventory.stock_allocation_completed.v1",
        DeliveryClass.CoreFlow,
        _occurredAt,
        """{"waveId":"abc"}""",
        "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
        "congo=t61rcWkgMzE",
        "22222222-2222-2222-2222-222222222222");

    [Fact]
    public void To_service_bus_message_carries_every_envelope_field()
    {
        var message = ServiceBusEnvelopeMapper.ToServiceBusMessage(FullEnvelope);

        message.MessageId.Should().Be(_eventId.ToString());
        message.Subject.Should().Be("inventory.stock_allocation_completed.v1");
        message.ContentType.Should().Be("application/json");
        message.Body.ToString().Should().Be("""{"waveId":"abc"}""");
        message.SessionId.Should().Be("22222222-2222-2222-2222-222222222222", "PartitionKey = kunci ordering session");
        message.ApplicationProperties["deliveryClass"].Should().Be("CoreFlow");
        message.ApplicationProperties["traceparent"].Should().Be("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
        message.ApplicationProperties["tracestate"].Should().Be("congo=t61rcWkgMzE");
        message.ApplicationProperties["partitionKey"].Should().Be("22222222-2222-2222-2222-222222222222");
    }

    [Fact]
    public void Session_id_falls_back_to_logical_name_when_partition_key_absent()
    {
        var envelope = FullEnvelope with { PartitionKey = null };

        var message = ServiceBusEnvelopeMapper.ToServiceBusMessage(envelope);

        message.SessionId.Should().Be(
            envelope.LogicalName,
            "entity bersession menuntut SessionId di tiap message, tanpa kunci aliran, ordering jatuh ke per event-type");
    }

    [Fact]
    public void Round_trip_through_a_received_message_is_lossless()
    {
        var outbound = ServiceBusEnvelopeMapper.ToServiceBusMessage(FullEnvelope);
        var received = AsReceivedMessage(outbound);

        var restored = ServiceBusEnvelopeMapper.ToEnvelope(received);

        restored.Should().Be(FullEnvelope, "record equality = semua field envelope bertahan");
    }

    [Fact]
    public void Round_trip_preserves_null_trace_and_partition_key()
    {
        var envelope = FullEnvelope with { Traceparent = null, Tracestate = null, PartitionKey = null };

        var outbound = ServiceBusEnvelopeMapper.ToServiceBusMessage(envelope);
        var restored = ServiceBusEnvelopeMapper.ToEnvelope(AsReceivedMessage(outbound));

        outbound.ApplicationProperties.Should().NotContainKeys(
            ["traceparent", "tracestate", "partitionKey"], "field null tidak ditulis sebagai properti kosong");
        restored.Should().Be(envelope);
    }

    // Simulasi message diterima: field yang sama dengan yang dikirim
    private static ServiceBusReceivedMessage AsReceivedMessage(ServiceBusMessage message) =>
        ServiceBusModelFactory.ServiceBusReceivedMessage(
            body: message.Body,
            messageId: message.MessageId,
            sessionId: message.SessionId,
            subject: message.Subject,
            contentType: message.ContentType,
            properties: message.ApplicationProperties);
}
