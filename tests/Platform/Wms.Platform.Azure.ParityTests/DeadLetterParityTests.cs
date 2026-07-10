using System.Collections.Concurrent;
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

// Message rusak masuk ke DLQ setelah batas retry, masih bisa dibaca untuk observability, dan tidak menghalangi message valid di session lain.
[Collection(ServiceBusEmulatorCollection.Name)]
public sealed class DeadLetterParityTests(ServiceBusEmulatorFixture emulator) : IAsyncLifetime
{
    private static readonly TimeSpan _receiveBudget = TimeSpan.FromSeconds(120);

    private ServiceBusClient _client = null!;
    private ServiceBusMessageSubscriber _subscriber = null!;
    private ServiceBusDeadLetterStore _deadLetterStore = null!;

    public Task InitializeAsync()
    {
        _client = new ServiceBusClient(emulator.ConnectionString);
        _subscriber = new ServiceBusMessageSubscriber(
            _client,
            new ServiceBusAdministrationClient(emulator.AdministrationConnectionString),
            Options.Create(new AzureMessagingOptions()),
            NullLogger<ServiceBusMessageSubscriber>.Instance);
        _deadLetterStore = new ServiceBusDeadLetterStore(_client, Options.Create(new AzureMessagingOptions()));
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _subscriber.DisposeAsync();
        await _client.DisposeAsync();
    }

    [Fact]
    public async Task Poison_message_lands_in_the_dlq_sub_queue_without_blocking_healthy_sessions()
    {
        var healthyProcessed = new ConcurrentQueue<Guid>();
        await _subscriber.SubscribeAsync(
            "wms.parity-poison",
            [new RailSubscription("parity.poison_checked.v1", DeliveryClass.CoreFlow)],
            (envelope, _) =>
            {
                healthyProcessed.Enqueue(envelope.EventId);
                return Task.FromResult(true);
            });

        // Kirim message rusak secara raw. Subject tetap cocok dengan rule, tapi mapper akan gagal dan message diulang sampai masuk DLQ.
        var poisonSender = _client.CreateSender(new AzureMessagingOptions().CoreFlowTopicName);
        await using (poisonSender.ConfigureAwait(false))
        {
            await poisonSender.SendMessageAsync(new ServiceBusMessage("bukan-envelope")
            {
                MessageId = "poison-1",
                Subject = "parity.poison_checked.v1",
                SessionId = "poison-session",
            });
        }

        var healthy = new MessageEnvelope(
            Guid.NewGuid(),
            "parity.poison_checked.v1",
            DeliveryClass.CoreFlow,
            DateTimeOffset.UtcNow,
            """{"ok":true}""",
            null,
            null,
            "healthy-session");
        var publisher = new ServiceBusMessagePublisher(_client, Options.Create(new AzureMessagingOptions()));
        await using (publisher.ConfigureAwait(false))
        {
            await publisher.PublishAsync(healthy);
        }

        IReadOnlyList<DeadLetteredMessage> deadLetters = [];
        await ParityWait.UntilAsync(
            async () =>
            {
                deadLetters = await _deadLetterStore.PeekAsync("wms.parity-poison");
                return deadLetters.Count > 0;
            },
            _receiveBudget,
            "message rusak masuk ke DLQ setelah batas retry");

        deadLetters.Should().ContainSingle(message => message.MessageId == "poison-1");
        var poison = deadLetters.Single(message => message.MessageId == "poison-1");
        poison.Reason.Should().Be("MaxDeliveryCountExceeded", "message diabandon berulang sampai broker memindahkannya ke DLQ");
        poison.DeliveryCount.Should().Be(ServiceBusRailTopology.MaxDeliveryCount);

        await ParityWait.UntilAsync(
            () => healthyProcessed.Contains(healthy.EventId),
            _receiveBudget,
            "pesan valid di session lain tetap diproses saat pesan rusak terus dicoba ulang");
    }
}
