namespace Wms.WebUI.Services;

// DTO modul Inventory. Dipisah dari InventoryApi supaya file typed-client fokus ke daftar endpoint.
public sealed record PutawayTaskDto(
    Guid PutawayTaskId,
    Guid StockId,
    Guid SourceLocationId,
    Guid SuggestedDestinationId,
    Guid AssignedTo,
    string Status,
    Guid? ActualDestinationId);

public sealed record CompletePutawayRequest(Guid ActualDestinationId, Guid? OperatorId);
