namespace Wms.Inventory.Application.ReadModels;

// Read DTO PutawayTask
public sealed record PutawayTaskDto(
    Guid PutawayTaskId,
    Guid StockId,
    Guid SourceLocationId,
    Guid SuggestedDestinationId,
    Guid AssignedTo,
    string Status,
    Guid? ActualDestinationId);
