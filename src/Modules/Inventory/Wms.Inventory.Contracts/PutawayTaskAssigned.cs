using Wms.Contracts.Abstractions;

namespace Wms.Inventory.Contracts;

// Inventory ke Notifications: PutawayTask dibuat (hanya Stock OnHand). Notification: alert operator terassign.
public sealed record PutawayTaskAssigned(
    Guid PutawayTaskId,
    Guid StockId,
    string Sku,
    Guid AssignedTo,
    Guid WarehouseId) : IIntegrationEvent
{
    public const string LogicalName = "inventory.putaway_task_assigned.v1";

    public const DeliveryClass DeliveryClass = DeliveryClass.Notification;
}
