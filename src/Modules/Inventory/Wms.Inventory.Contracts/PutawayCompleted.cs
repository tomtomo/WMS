using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Inventory.Contracts;

// Inventory ke Reporting: putaway selesai (Stock OnHand ke Available).
public sealed record PutawayCompleted(
    Guid PutawayTaskId,
    Guid StockId,
    string Sku,
    Guid WarehouseId,
    Guid? OperatorId) : IIntegrationEvent
{
    public const string LogicalName = "inventory.putaway_completed.v1";

    public const DeliveryClass DeliveryClass = DeliveryClass.CoreFlow;
}
