using AwesomeAssertions;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using NSubstitute;
using Wms.Contracts.Abstractions;
using Wms.Platform.Azure.Messaging;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Mapping stream publisher offline: partition key dari deklarasi kontrak, producer dicache per stream.
public sealed class StreamPublisherMappingTests
{
    private readonly EventHubProducerClient _producer = Substitute.For<EventHubProducerClient>();
    private readonly List<string> _createdStreams = [];
    private readonly EventHubsEventStreamPublisher _publisher;

    public StreamPublisherMappingTests()
    {
        _publisher = new EventHubsEventStreamPublisher(streamName =>
        {
            _createdStreams.Add(streamName);
            return _producer;
        });
    }

    [Fact]
    public async Task Partition_key_flows_from_the_contract_declaration()
    {
        SendEventOptions? sendOptions = null;
        await CaptureSendOptionsAsync(options => sendOptions = options);

        await _publisher.PublishAsync("wms-scan-stream", new OrderedScanEvent(Guid.Parse("66666666-6666-6666-6666-666666666666")));

        sendOptions.Should().NotBeNull();
        sendOptions!.PartitionKey.Should().Be("66666666-6666-6666-6666-666666666666");
    }

    [Fact]
    public async Task Payload_without_a_declared_key_is_round_robined()
    {
        SendEventOptions? sendOptions = null;
        await CaptureSendOptionsAsync(options => sendOptions = options);

        await _publisher.PublishAsync("wms-scan-stream", new PlainScanEvent("SKU-1"));

        sendOptions.Should().NotBeNull();
        sendOptions!.PartitionKey.Should().BeNull();
    }

    [Fact]
    public async Task Producer_is_created_once_per_stream()
    {
        await _publisher.PublishAsync("wms-scan-stream", new PlainScanEvent("SKU-1"));
        await _publisher.PublishAsync("wms-scan-stream", new PlainScanEvent("SKU-2"));
        await _publisher.PublishAsync("wms-audit-stream", new PlainScanEvent("SKU-3"));

        _createdStreams.Should().Equal("wms-scan-stream", "wms-audit-stream");
    }

    private Task CaptureSendOptionsAsync(Action<SendEventOptions> capture)
    {
        _producer.SendAsync(
                Arg.Any<IEnumerable<EventData>>(),
                Arg.Do<SendEventOptions>(capture),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        return Task.CompletedTask;
    }

    private sealed record OrderedScanEvent(Guid StockId) : IHasPartitionKey
    {
        string IHasPartitionKey.PartitionKey => StockId.ToString();
    }

    private sealed record PlainScanEvent(string Sku);
}
