using Wms.BuildingBlocks.Domain.Events;

namespace Wms.Outbound.Domain.Events;

// Task picking dibuat dan diassign ke operator
public sealed record PickingTaskAssignedRaised(
    PickingTaskId TaskId,
    WaveId WaveId,
    Guid StockId,
    Guid ReservationId,
    string Sku,
    Guid AssignedTo) : IDomainEvent;
