using Wms.Contracts.Abstractions;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestDoubles;

// Test Event dengan PartitionKey dari StreamId.
public sealed record OrderedStreamTestEvent(Guid StreamId) : IIntegrationEvent, IHasPartitionKey
{
    public const string LogicalName = "inbound.ordered_stream.v1";

    string IHasPartitionKey.PartitionKey => StreamId.ToString();
}
