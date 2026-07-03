using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.BuildingBlocks.Infrastructure.IntegrationTests.TestDoubles;

public sealed record GoodsReceivedTestEvent(string ReceiptId, int Quantity) : IIntegrationEvent
{
    public const string LogicalName = "inbound.gr_confirmed.v1";
}
