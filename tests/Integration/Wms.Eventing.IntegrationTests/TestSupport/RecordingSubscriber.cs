using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Platform.Local.Messaging;

namespace Wms.Eventing.IntegrationTests.TestSupport;

// Subscriber transport langsung untuk membuktikan routing per deliveryClass tanpa domain setup
internal sealed class RecordingSubscriber : IDisposable
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqMessageSubscriber _subscriber;

    public RecordingSubscriber(string rabbitConnectionString, string exchange)
    {
        var options = Options.Create(new RabbitMqOptions { ExchangeName = exchange, ConnectionStringName = "rabbitmq" });
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:rabbitmq", rabbitConnectionString)])
            .Build();
        _connectionFactory = new RabbitMqConnectionFactory(configuration, options);
        _subscriber = new RabbitMqMessageSubscriber(
            _connectionFactory, options, NullLogger<RabbitMqMessageSubscriber>.Instance);
    }

    public ConcurrentBag<MessageEnvelope> Received { get; } = [];

    public Task StartAsync(string queueName, IReadOnlyCollection<RailSubscription> subscriptions) =>
        _subscriber.SubscribeAsync(queueName, subscriptions, (envelope, _) =>
        {
            Received.Add(envelope);
            return Task.FromResult(true);
        });

    public void Dispose()
    {
        _subscriber.Dispose();
        _connectionFactory.Dispose();
    }
}
