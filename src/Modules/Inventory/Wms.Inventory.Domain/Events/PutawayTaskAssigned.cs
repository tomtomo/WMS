using Wms.BuildingBlocks.Domain.Events;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Domain.Events;

// PutawayTask dibuat
public sealed record PutawayTaskAssigned(
    PutawayTaskId PutawayTaskId,
    StockId StockId,
    Guid AssignedTo,
    LocationId SuggestedDestinationId) : IDomainEvent;
