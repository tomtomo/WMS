using System.Text.Json;
using AwesomeAssertions;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestDoubles;
using Xunit;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests;

// Test MessageEnvelope
public sealed class MessageEnvelopeTests
{
    private static readonly Guid _eventId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset _occurredAt = new(2026, 7, 3, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_wraps_metadata_and_serializes_payload_to_json()
    {
        var payload = new GoodsReceivedTestEvent("GR-1", 5);

        var envelope = MessageEnvelope.Create(
            payload,
            "inbound.gr_confirmed.v1",
            DeliveryClass.CoreFlow,
            _eventId,
            _occurredAt);

        envelope.EventId.Should().Be(_eventId);
        envelope.LogicalName.Should().Be("inbound.gr_confirmed.v1");
        envelope.DeliveryClass.Should().Be(DeliveryClass.CoreFlow);
        envelope.OccurredAt.Should().Be(_occurredAt);
        envelope.Payload.Should().Contain("GR-1").And.Contain("5");
        envelope.Traceparent.Should().BeNull();
        envelope.Tracestate.Should().BeNull();
    }

    [Fact]
    public void Payload_round_trips_back_to_an_identical_event()
    {
        var payload = new GoodsReceivedTestEvent("GR-42", 12);

        var envelope = MessageEnvelope.Create(
            payload,
            "inbound.gr_confirmed.v1",
            DeliveryClass.CoreFlow,
            _eventId,
            _occurredAt);

        var restored = JsonSerializer.Deserialize<GoodsReceivedTestEvent>(
            envelope.Payload,
            MessageEnvelope.PayloadSerializerOptions);

        restored.Should().Be(payload);
    }

    [Theory]
    [InlineData("inbound.gr_confirmed.v1")]
    [InlineData("inventory.stock_allocation_completed.v1")]
    [InlineData("outbound.wave_released.v12")]
    [InlineData("auth.user_registered.v1")]
    public void Create_accepts_well_formed_logical_name(string logicalName)
    {
        var act = () => MessageEnvelope.Create(
            new GoodsReceivedTestEvent("GR-1", 1),
            logicalName,
            DeliveryClass.CoreFlow,
            _eventId,
            _occurredAt);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("Inbound.gr_confirmed.v1")]
    [InlineData("inbound.GrConfirmed.v1")]
    [InlineData("inbound.gr_confirmed")]
    [InlineData("inbound.gr_confirmed.v")]
    [InlineData("inbound.gr_confirmed.1")]
    [InlineData("inbound..v1")]
    [InlineData("gr_confirmed.v1")]
    [InlineData("inbound.gr.confirmed.v1")]
    public void Create_rejects_logical_name_that_violates_module_event_version_format(string logicalName)
    {
        var act = () => MessageEnvelope.Create(
            new GoodsReceivedTestEvent("GR-1", 1),
            logicalName,
            DeliveryClass.CoreFlow,
            _eventId,
            _occurredAt);

        act.Should().Throw<ArgumentException>();
    }
}
