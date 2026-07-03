using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using RabbitMQ.Client.Exceptions;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Platform.Local.IntegrationTests.TestSupport;
using Wms.Platform.Local.Messaging;
using Xunit;

namespace Wms.Platform.Local.IntegrationTests;

[Collection(RabbitMqCollection.Name)]
public sealed class RabbitMqMessagingTests(RabbitMqFixture fixture)
{
    private static readonly TimeSpan _receiveTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task Publish_with_confirms_then_consume_round_trips_envelope_payload()
    {
        var options = CreateOptions();
        using var connectionFactory = CreateConnectionFactory(fixture.ConnectionString, options);
        using var subscriber = new RabbitMqMessageSubscriber(
            connectionFactory, options, new CapturingLogger<RabbitMqMessageSubscriber>());
        var publisher = new RabbitMqMessagePublisher(connectionFactory, options, TimeProvider.System);
        var received = new TaskCompletionSource<OrderShippedTestEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        await subscriber.SubscribeAsync<OrderShippedTestEvent>((integrationEvent, _) =>
        {
            received.TrySetResult(integrationEvent);
            return Task.CompletedTask;
        });

        var sent = new OrderShippedTestEvent(Guid.NewGuid(), "JNE");
        await publisher.PublishAsync(sent, DeliveryClass.CoreFlow);

        (await received.Task.WaitAsync(_receiveTimeout)).Should().Be(sent);
    }

    [Fact]
    public async Task Poison_message_is_nacked_without_requeue_and_does_not_block_queue()
    {
        var options = CreateOptions();
        using var connectionFactory = CreateConnectionFactory(fixture.ConnectionString, options);
        using var subscriber = new RabbitMqMessageSubscriber(
            connectionFactory, options, new CapturingLogger<RabbitMqMessageSubscriber>());
        var publisher = new RabbitMqMessagePublisher(connectionFactory, options, TimeProvider.System);
        var poisonDeliveries = 0;
        var healthyReceived = new TaskCompletionSource<OrderShippedTestEvent>(TaskCreationOptions.RunContinuationsAsynchronously);
        await subscriber.SubscribeAsync<OrderShippedTestEvent>((integrationEvent, _) =>
        {
            if (integrationEvent.Carrier == "poison")
            {
                Interlocked.Increment(ref poisonDeliveries);
                throw new InvalidOperationException("handler gagal memproses");
            }

            healthyReceived.TrySetResult(integrationEvent);
            return Task.CompletedTask;
        });

        await publisher.PublishAsync(new OrderShippedTestEvent(Guid.NewGuid(), "poison"), DeliveryClass.CoreFlow);
        var healthy = new OrderShippedTestEvent(Guid.NewGuid(), "SiCepat");
        await publisher.PublishAsync(healthy, DeliveryClass.CoreFlow);

        (await healthyReceived.Task.WaitAsync(_receiveTimeout)).Should().Be(healthy);
        poisonDeliveries.Should().Be(1);
    }

    [Fact]
    public async Task Publish_to_unreachable_broker_fails_loud()
    {
        var options = CreateOptions();
        using var connectionFactory = CreateConnectionFactory("amqp://guest:guest@localhost:1", options);
        var publisher = new RabbitMqMessagePublisher(connectionFactory, options, TimeProvider.System);

        var act = () => publisher.PublishAsync(new OrderShippedTestEvent(Guid.NewGuid(), "JNE"), DeliveryClass.CoreFlow);

        await act.Should().ThrowAsync<BrokerUnreachableException>();
    }

    // Exchange + prefix unik per test: satu container dipakai bersama tanpa saling ganggu.
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
