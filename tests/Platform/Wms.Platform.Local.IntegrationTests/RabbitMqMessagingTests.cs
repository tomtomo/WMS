using System.Text.Json;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Exceptions;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Contracts.Abstractions;
using Wms.Platform.Local.IntegrationTests.TestSupport;
using Wms.Platform.Local.Messaging;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests;

// Test publish dan subscribe message melalui RabbitMQ.
[Collection(RabbitMqCollection.Name)]
public sealed class RabbitMqMessagingTests(RabbitMqFixture fixture)
{
    private static readonly TimeSpan _receiveTimeout = TimeSpan.FromSeconds(30);
    private static readonly DateTimeOffset _occurredAt = new(2026, 7, 6, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Publish_with_confirms_then_consume_round_trips_envelope()
    {
        var options = CreateOptions();
        using var connectionFactory = CreateConnectionFactory(fixture.ConnectionString, options);
        using var subscriber = new RabbitMqMessageSubscriber(
            connectionFactory, options, new CapturingLogger<RabbitMqMessageSubscriber>());
        var publisher = new RabbitMqMessagePublisher(connectionFactory, options);
        var received = new TaskCompletionSource<MessageEnvelope>(TaskCreationOptions.RunContinuationsAsynchronously);
        await subscriber.SubscribeAsync(
            options.Value.SubscriberQueuePrefix,
            [new RailSubscription(OrderShippedTestEvent.LogicalName, DeliveryClass.CoreFlow)],
            (envelope, _) =>
            {
                received.TrySetResult(envelope);
                return Task.FromResult(true);
            });

        var sentEvent = new OrderShippedTestEvent(Guid.NewGuid(), "JNE");
        var sent = Envelope(sentEvent, DeliveryClass.CoreFlow);
        await publisher.PublishAsync(sent);

        var got = await received.Task.WaitAsync(_receiveTimeout);
        got.EventId.Should().Be(sent.EventId);
        got.LogicalName.Should().Be(sent.LogicalName);
        got.DeliveryClass.Should().Be(DeliveryClass.CoreFlow);
        got.OccurredAt.Should().Be(_occurredAt);
        JsonSerializer.Deserialize<OrderShippedTestEvent>(got.Payload, MessageEnvelope.PayloadSerializerOptions)
            .Should().Be(sentEvent);
    }

    [Fact]
    public async Task Poison_message_is_nacked_without_requeue_and_does_not_block_queue()
    {
        var options = CreateOptions();
        using var connectionFactory = CreateConnectionFactory(fixture.ConnectionString, options);
        using var subscriber = new RabbitMqMessageSubscriber(
            connectionFactory, options, new CapturingLogger<RabbitMqMessageSubscriber>());
        var publisher = new RabbitMqMessagePublisher(connectionFactory, options);
        var poisonDeliveries = 0;
        var healthyReceived = new TaskCompletionSource<OrderShippedTestEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        await subscriber.SubscribeAsync(
            options.Value.SubscriberQueuePrefix,
            [new RailSubscription(OrderShippedTestEvent.LogicalName, DeliveryClass.CoreFlow)],
            (envelope, _) =>
            {
                var integrationEvent = JsonSerializer.Deserialize<OrderShippedTestEvent>(
                    envelope.Payload, MessageEnvelope.PayloadSerializerOptions)!;
                if (integrationEvent.Carrier == "poison")
                {
                    Interlocked.Increment(ref poisonDeliveries);
                    throw new InvalidOperationException("handler gagal memproses");
                }

                healthyReceived.TrySetResult(integrationEvent);
                return Task.FromResult(true);
            });

        await publisher.PublishAsync(Envelope(new OrderShippedTestEvent(Guid.NewGuid(), "poison"), DeliveryClass.CoreFlow));
        var healthy = new OrderShippedTestEvent(Guid.NewGuid(), "SiCepat");
        await publisher.PublishAsync(Envelope(healthy, DeliveryClass.CoreFlow));

        (await healthyReceived.Task.WaitAsync(_receiveTimeout)).Should().Be(healthy);
        poisonDeliveries.Should().Be(1);
    }

    [Fact]
    public async Task Publish_to_unreachable_broker_fails_loud()
    {
        var options = CreateOptions();
        using var connectionFactory = CreateConnectionFactory("amqp://guest:guest@localhost:1", options);
        var publisher = new RabbitMqMessagePublisher(connectionFactory, options);

        var act = () => publisher.PublishAsync(Envelope(new OrderShippedTestEvent(Guid.NewGuid(), "JNE"), DeliveryClass.CoreFlow));

        await act.Should().ThrowAsync<BrokerUnreachableException>();
    }

    private static MessageEnvelope Envelope(OrderShippedTestEvent integrationEvent, DeliveryClass deliveryClass) =>
        MessageEnvelope.Create(
            integrationEvent, OrderShippedTestEvent.LogicalName, deliveryClass, Guid.NewGuid(), _occurredAt);

    // Gunakan exchange dan queue prefix unik agar tiap test terisolasi.
    private static IOptions<RabbitMqOptions> CreateOptions()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        return Options.Create(new RabbitMqOptions
        {
            ExchangeName = $"wms.events.it-{unique}",
            SubscriberQueuePrefix = $"it-{unique}",
        });
    }

    private static RabbitMqConnectionFactory CreateConnectionFactory(string connectionString, IOptions<RabbitMqOptions> options)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:rabbitmq", connectionString)])
            .Build();
        return new RabbitMqConnectionFactory(configuration, options);
    }
}
