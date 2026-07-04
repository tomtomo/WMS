using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Outbound.Contracts;

// Outbound ke Inventory/Reporting: picking selesai. Inventory: reservasi Fulfilled dan split fisik ke Picked.
public sealed record PickingCompleted(
    Guid WaveId,
    Guid PickingTaskId,
    Guid StockId,
    Guid ReservationId,
    string Sku,
    string? Batch,
    decimal Qty,
    Guid StagingLocationId,
    Guid? OperatorId) : IIntegrationEvent
{
    public const string LogicalName = "outbound.picking_completed.v1";

    public const DeliveryClass DeliveryClass = DeliveryClass.CoreFlow;
}
