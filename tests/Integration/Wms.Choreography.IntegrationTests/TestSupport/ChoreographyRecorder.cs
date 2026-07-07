using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Platform.Local.Messaging;

namespace Wms.Choreography.IntegrationTests.TestSupport;

// Mencatat urutan event untuk kebutuhan test
internal sealed class ChoreographyRecorder : IDisposable
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly RabbitMqMessageSubscriber _subscriber;
    private readonly object _gate = new();
    private readonly List<MessageEnvelope> _received = [];

    public ChoreographyRecorder(string rabbitConnectionString, string exchange)
    {
        var options = Options.Create(new RabbitMqOptions { ExchangeName = exchange, ConnectionStringName = "rabbitmq" });
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([new KeyValuePair<string, string?>("ConnectionStrings:rabbitmq", rabbitConnectionString)])
            .Build();
        _connectionFactory = new RabbitMqConnectionFactory(configuration, options);
        _subscriber = new RabbitMqMessageSubscriber(
            _connectionFactory, options, NullLogger<RabbitMqMessageSubscriber>.Instance);
    }

    public Task StartAsync(string queueName, IReadOnlyCollection<RailSubscription> subscriptions) =>
        _subscriber.SubscribeAsync(queueName, subscriptions, (envelope, _) =>
        {
            lock (_gate)
            {
                _received.Add(envelope);
            }

            return Task.FromResult(true);
        });

    // Urutan logical name
    public IReadOnlyList<string> ObservedLogicalNames()
    {
        lock (_gate)
        {
            return [.. _received.Select(envelope => envelope.LogicalName)];
        }
    }

    public IReadOnlyList<MessageEnvelope> Snapshot()
    {
        lock (_gate)
        {
            return [.. _received];
        }
    }

    public void Dispose()
    {
        _subscriber.Dispose();
        _connectionFactory.Dispose();
    }
}
