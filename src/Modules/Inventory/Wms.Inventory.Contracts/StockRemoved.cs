using Wms.BuildingBlocks.Application.Messaging;
using Wms.Inventory.Contracts.Payloads;

namespace Wms.Inventory.Contracts;

// Inventory ke Reporting: stok Picked dihapus setelah ShipmentDispatched. Diemit Inventory, bukan diturunkan dari event Outbound.
public sealed record StockRemoved(
    Guid WaveId,
    IReadOnlyList<StockRemovedLine> Lines) : IIntegrationEvent
{
    public const string LogicalName = "inventory.stock_removed.v1";

    public const DeliveryClass DeliveryClass = DeliveryClass.CoreFlow;
}
