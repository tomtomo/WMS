using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Platform.Local.IntegrationTests.TestSupport;

// Integration event test dengan identitas broker sesuai konvensi LogicalName.
public sealed record OrderShippedTestEvent(Guid OrderId, string Carrier) : IIntegrationEvent
{
    public const string LogicalName = "outbound.order_shipped_test.v1";
}
