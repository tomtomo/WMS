namespace Wms.Outbound.Application.ReadModels;

// Read DTO PickingTask
public sealed record PickingTaskDto(
    Guid PickingTaskId,
    Guid WaveId,
    Guid StockId,
    Guid SourceLocationId,
    string Sku,
    string? Batch,
    decimal Qty,
    Guid AssignedTo,
    string Status,
    decimal? ActualQty,
    Guid? StagingLocationId);
