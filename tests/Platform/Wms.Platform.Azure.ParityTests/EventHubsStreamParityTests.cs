using AwesomeAssertions;
using Azure.Identity;
using Azure.Messaging.EventHubs.Consumer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Wms.Contracts.Abstractions;
using Wms.Platform.Azure.Messaging;
using Wms.Platform.Azure.ParityTests.TestSupport;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Test stream lewat emulator Event Hubs: event yang dipublish bisa dibaca lagi, dan PartitionKey menentukan partisinya.
[Collection(EventHubsEmulatorCollection.Name)]
public sealed class EventHubsStreamParityTests(EventHubsEmulatorFixture emulator)
{
    [Fact]
    public async Task Published_events_are_read_back_through_the_consumer_group_with_partition_affinity()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:eventhubs"] = emulator.ConnectionString,
            })
            .Build();
        var publisher = new EventHubsEventStreamPublisher(
            Options.Create(new AzureMessagingOptions()), configuration, new DefaultAzureCredential());

        await using (publisher.ConfigureAwait(false))
        {
            var stockId = Guid.Parse("99999999-9999-9999-9999-999999999999");
            await publisher.PublishAsync(EventHubsEmulatorFixture.StreamName, new OrderedScan(stockId, 1));
            await publisher.PublishAsync(EventHubsEmulatorFixture.StreamName, new OrderedScan(stockId, 2));
            await publisher.PublishAsync(EventHubsEmulatorFixture.StreamName, new PlainScan("SKU-1"));
        }

        var received = new List<PartitionEvent>();
        var consumer = new EventHubConsumerClient(
            EventHubConsumerClient.DefaultConsumerGroupName,
            emulator.ConnectionString,
            EventHubsEmulatorFixture.StreamName);
        await using (consumer.ConfigureAwait(false))
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await foreach (var partitionEvent in consumer.ReadEventsAsync(
                startReadingAtEarliestEvent: true,
                new ReadEventOptions { MaximumWaitTime = TimeSpan.FromSeconds(2) },
                timeout.Token))
            {
                if (partitionEvent.Data is null)
                {
                    continue;
                }

                received.Add(partitionEvent);
                if (received.Count == 3)
                {
                    break;
                }
            }
        }

        received.Should().HaveCount(3, "publish batch terbaca via consumer group");
        var orderedPartitions = received
            .Where(partitionEvent => partitionEvent.Data.PartitionKey is not null)
            .Select(partitionEvent => partitionEvent.Partition.PartitionId)
            .Distinct()
            .ToList();
        orderedPartitions.Should().HaveCount(1, "payload dengan partition key sama mendarat di partisi yang sama");
    }

    private sealed record OrderedScan(Guid StockId, int Seq) : IHasPartitionKey
    {
        string IHasPartitionKey.PartitionKey => StockId.ToString();
    }

    private sealed record PlainScan(string Sku);
}
