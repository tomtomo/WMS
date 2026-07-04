using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Outbound.Contracts;

// Outbound ke Notifications: PickingTask dibuat (satu per allocation). Notification: alert operator terassign.
public sealed record PickingTaskAssigned(
    Guid PickingTaskId,
    Guid WaveId,
    Guid StockId,
    string Sku,
    Guid AssignedTo,
    Guid WarehouseId) : IIntegrationEvent
{
    public const string LogicalName = "outbound.picking_task_assigned.v1";

    public const DeliveryClass DeliveryClass = DeliveryClass.Notification;
}
