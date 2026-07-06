using AwesomeAssertions;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Infrastructure.Outbox;
using Xunit;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests;

// Test OutboxDispatcher base
public sealed class OutboxDispatcherTests
{
    [Theory]
    [InlineData(DeliveryClass.CoreFlow)]
    [InlineData(DeliveryClass.Notification)]
    public async Task Routes_each_envelope_to_the_rail_matching_its_delivery_class(DeliveryClass deliveryClass)
    {
        var dispatcher = new RecordingDispatcher(Result.Success());

        await dispatcher.DispatchAsync(EnvelopeFor(deliveryClass));

        dispatcher.RoutedTo.Should().Be(deliveryClass);
    }

    [Fact]
    public async Task Throws_when_a_publish_returns_failure()
    {
        var dispatcher = new RecordingDispatcher(Result.Failure(new Error("broker.unavailable", "down")));

        var dispatch = async () => await dispatcher.DispatchAsync(EnvelopeFor(DeliveryClass.CoreFlow));

        await dispatch.Should().ThrowAsync<OutboxDispatchException>();
    }

    private static MessageEnvelope EnvelopeFor(DeliveryClass deliveryClass) => new(
        Guid.NewGuid(),
        "inbound.gr_confirmed.v1",
        deliveryClass,
        DateTimeOffset.UnixEpoch,
        "{}",
        null,
        null);

    private sealed class RecordingDispatcher(Result result) : OutboxDispatcher
    {
        public DeliveryClass? RoutedTo { get; private set; }

        protected override Task<Result> PublishToCoreFlowAsync(
            MessageEnvelope envelope,
            CancellationToken cancellationToken)
        {
            RoutedTo = DeliveryClass.CoreFlow;
            return Task.FromResult(result);
        }

        protected override Task<Result> PublishToNotificationAsync(
            MessageEnvelope envelope,
            CancellationToken cancellationToken)
        {
            RoutedTo = DeliveryClass.Notification;
            return Task.FromResult(result);
        }
    }
}
