using AwesomeAssertions;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using NSubstitute;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Contracts.Abstractions;
using Wms.Platform.Azure.Eventing;
using Wms.Platform.Azure.Messaging;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Dispatcher yang sama mengirim CoreFlow ke Service Bus dan Notification ke Event Grid.
// Test berjalan tanpa broker dengan mock client SDK Azure.
public sealed class RailSplitRoutingTests
{
    private readonly ServiceBusSender _sender = Substitute.For<ServiceBusSender>();
    private readonly ServiceBusClient _serviceBusClient = Substitute.For<ServiceBusClient>();
    private readonly EventGridPublisherClient _eventGridClient = Substitute.For<EventGridPublisherClient>();
    private readonly AzureOutboxDispatcher _dispatcher;

    public RailSplitRoutingTests()
    {
        _serviceBusClient.CreateSender("wms-core-flow").Returns(_sender);
        _dispatcher = new AzureOutboxDispatcher(
            new ServiceBusMessagePublisher(_serviceBusClient, Options.Create(new AzureMessagingOptions())),
            new EventGridNotificationPublisher(_eventGridClient));
    }

    [Fact]
    public async Task Core_flow_envelope_goes_to_service_bus_not_event_grid()
    {
        await _dispatcher.DispatchAsync(Envelope(DeliveryClass.CoreFlow));

        await _sender.Received(1).SendMessageAsync(Arg.Any<ServiceBusMessage>(), Arg.Any<CancellationToken>());
        await _eventGridClient.DidNotReceiveWithAnyArgs().SendEventAsync(default(CloudEvent)!);
    }

    [Fact]
    public async Task Notification_envelope_goes_to_event_grid_not_service_bus()
    {
        await _dispatcher.DispatchAsync(Envelope(DeliveryClass.Notification));

        await _eventGridClient.Received(1).SendEventAsync(Arg.Any<CloudEvent>(), Arg.Any<CancellationToken>());
        await _sender.DidNotReceiveWithAnyArgs().SendMessageAsync(default!);
    }

    [Fact]
    public async Task Dual_class_pair_lands_on_both_rails_without_dedup()
    {
        // Dual class = dua baris outbox (dua envelope, dua eventId) untuk payload yang sama.
        var coreFlow = Envelope(DeliveryClass.CoreFlow);
        var notification = Envelope(DeliveryClass.Notification);

        await _dispatcher.DispatchAsync(coreFlow);
        await _dispatcher.DispatchAsync(notification);

        await _sender.Received(1).SendMessageAsync(
            Arg.Is<ServiceBusMessage>(message => message.MessageId == coreFlow.EventId.ToString()),
            Arg.Any<CancellationToken>());
        await _eventGridClient.Received(1).SendEventAsync(
            Arg.Is<CloudEvent>(cloudEvent => cloudEvent.Id == notification.EventId.ToString()),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Service_bus_publisher_rejects_a_notification_envelope()
    {
        var publisher = new ServiceBusMessagePublisher(_serviceBusClient, Options.Create(new AzureMessagingOptions()));

        var publish = async () => await publisher.PublishAsync(Envelope(DeliveryClass.Notification));

        await publish.Should().ThrowAsync<InvalidOperationException>("publisher core-flow menolak salah-rail");
    }

    [Fact]
    public async Task Event_grid_publisher_rejects_a_core_flow_envelope()
    {
        var publisher = new EventGridNotificationPublisher(_eventGridClient);

        var publish = async () => await publisher.PublishAsync(Envelope(DeliveryClass.CoreFlow));

        await publish.Should().ThrowAsync<InvalidOperationException>("publisher notification menolak salah-rail");
    }

    private static MessageEnvelope Envelope(DeliveryClass deliveryClass) => new(
        Guid.NewGuid(),
        "inventory.stock_allocation_completed.v1",
        deliveryClass,
        DateTimeOffset.UnixEpoch,
        """{"waveId":"abc"}""",
        null,
        null,
        "wave-1");
}
