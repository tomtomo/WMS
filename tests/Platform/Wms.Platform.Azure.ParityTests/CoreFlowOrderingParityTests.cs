using System.Collections.Concurrent;
using System.Text.Json;
using AwesomeAssertions;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Contracts.Abstractions;
using Wms.Platform.Azure.Messaging;
using Wms.Platform.Azure.ParityTests.TestSupport;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Test rail core flow lewat emulator Service Bus: urutan dijaga per session, eventId tetap untuk cegah proses ulang di Inbox.
[Collection(ServiceBusEmulatorCollection.Name)]
public sealed class CoreFlowOrderingParityTests(ServiceBusEmulatorFixture emulator) : IAsyncLifetime
{
    private static readonly TimeSpan _receiveBudget = TimeSpan.FromSeconds(90);

    private ServiceBusClient _client = null!;
    private ServiceBusMessageSubscriber _subscriber = null!;
    private ServiceBusMessagePublisher _publisher = null!;

    public Task InitializeAsync()
    {
        _client = new ServiceBusClient(emulator.ConnectionString);
        _subscriber = new ServiceBusMessageSubscriber(
            _client,
            new ServiceBusAdministrationClient(emulator.AdministrationConnectionString),
            Options.Create(new AzureMessagingOptions()),
            NullLogger<ServiceBusMessageSubscriber>.Instance);
        _publisher = new ServiceBusMessagePublisher(_client, Options.Create(new AzureMessagingOptions()));
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _subscriber.DisposeAsync();
        await _publisher.DisposeAsync();
        await _client.DisposeAsync();
    }

    [Fact]
    public async Task Messages_of_one_partition_key_are_processed_in_publish_order()
    {
        var received = new ConcurrentQueue<int>();
        await _subscriber.SubscribeAsync(
            "wms.parity-ordering",
            [new RailSubscription("parity.ordering_checked.v1", DeliveryClass.CoreFlow)],
            (envelope, _) =>
            {
                received.Enqueue(JsonDocument.Parse(envelope.Payload).RootElement.GetProperty("seq").GetInt32());
                return Task.FromResult(true);
            });

        foreach (var sequence in Enumerable.Range(1, 5))
        {
            await _publisher.PublishAsync(Envelope("parity.ordering_checked.v1", $$"""{"seq":{{sequence}}}""", "wave-ordering-1"));
        }

        await ParityWait.UntilAsync(() => received.Count == 5, _receiveBudget, "5 pesan satu session diterima");

        // Satu partition key = satu session = FIFO.
        received.Should().Equal(1, 2, 3, 4, 5);
    }

    [Fact]
    public async Task Replayed_event_keeps_its_event_id_so_inbox_dedup_still_holds()
    {
        var deliveries = new ConcurrentQueue<Guid>();
        var inbox = new ConcurrentDictionary<Guid, bool>();
        var processed = 0;
        await _subscriber.SubscribeAsync(
            "wms.parity-dedup",
            [new RailSubscription("parity.dedup_checked.v1", DeliveryClass.CoreFlow)],
            (envelope, _) =>
            {
                deliveries.Enqueue(envelope.EventId);

                if (inbox.TryAdd(envelope.EventId, true))
                {
                    Interlocked.Increment(ref processed);
                }

                return Task.FromResult(true);
            });

        var replayed = Envelope("parity.dedup_checked.v1", """{"seq":1}""", "wave-dedup-1");
        await _publisher.PublishAsync(replayed);
        await _publisher.PublishAsync(replayed);

        await ParityWait.UntilAsync(() => deliveries.Count == 2, _receiveBudget, "replay terkirim dua kali");
        deliveries.Should().OnlyContain(eventId => eventId == replayed.EventId, "redelivery tidak boleh mengganti eventId");
        processed.Should().Be(1, "Inbox memakai eventId, jadi replay tidak diproses ulang");
    }

    private static MessageEnvelope Envelope(string logicalName, string payload, string partitionKey) => new(
        Guid.NewGuid(),
        logicalName,
        DeliveryClass.CoreFlow,
        DateTimeOffset.UtcNow,
        payload,
        null,
        null,
        partitionKey);
}
