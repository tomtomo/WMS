using System.Collections.Concurrent;
using System.Text.Json;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Contracts.Abstractions;

namespace Wms.Platform.Azure.Messaging;

// Publisher untuk stream event bisnis lewat Event Hubs. Jika payload punya partition key, event dikirim sesuai key tersebut.
public sealed class EventHubsEventStreamPublisher : IEventStreamPublisher, IAsyncDisposable
{
    // Lazy per key: factory GetOrAdd berjalan di luar lock.
    private readonly ConcurrentDictionary<string, Lazy<EventHubProducerClient>> _producers = new(StringComparer.Ordinal);
    private readonly Func<string, EventHubProducerClient> _producerFactory;

    public EventHubsEventStreamPublisher(IOptions<AzureMessagingOptions> options, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configuration);

        // Lazy per hub: Producer dibuat per hub saat publish pertama, supaya constructor tidak langsung membuka koneksi jaringan.
        _producerFactory = streamName =>
            new EventHubProducerClient(ResolveConnectionString(options.Value, configuration), streamName);
    }

    // Constructor khusus test agar producer bisa diganti tanpa akses jaringan.
    internal EventHubsEventStreamPublisher(Func<string, EventHubProducerClient> producerFactory) =>
        _producerFactory = producerFactory;

    public async Task PublishAsync<TEvent>(string streamName, TEvent payload, CancellationToken cancellationToken = default)
        where TEvent : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamName);
        ArgumentNullException.ThrowIfNull(payload);

        var producer = _producers
            .GetOrAdd(streamName, name => new Lazy<EventHubProducerClient>(() => _producerFactory(name)))
            .Value;
        var eventData = new EventData(JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType()))
        {
            ContentType = "application/json",
        };

        // Tanpa partition key, broker bebas membagi event. Urutan per aliran hanya dijaga untuk payload yang menyediakan partition key.
        var sendOptions = new SendEventOptions { PartitionKey = (payload as IHasPartitionKey)?.PartitionKey };
        await producer.SendAsync([eventData], sendOptions, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var producer in _producers.Values.Where(lazy => lazy.IsValueCreated))
        {
            await producer.Value.DisposeAsync().ConfigureAwait(false);
        }
    }

    private static string ResolveConnectionString(AzureMessagingOptions options, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(options.EventHubsConnectionStringName);
        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException(
                $"Connection string '{options.EventHubsConnectionStringName}' untuk Event Hubs tidak ditemukan di konfigurasi.")
            : connectionString;
    }
}
