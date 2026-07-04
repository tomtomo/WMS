using Wms.BuildingBlocks.Domain.Events;

namespace Wms.Inventory.Domain.Events;

// Split fisik saat picking
public sealed record StockPicked(
    StockId SourceStockId,
    StockId PickedStockId,
    StockReservationId ReservationId,
    Guid PickingTaskId,
    decimal Qty) : IDomainEvent;
