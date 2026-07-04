using Wms.BuildingBlocks.Domain.Events;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Domain.Events;

// PutawayTask selesai
public sealed record PutawayTaskCompleted(
    PutawayTaskId PutawayTaskId,
    StockId StockId,
    LocationId ActualDestinationId) : IDomainEvent;
