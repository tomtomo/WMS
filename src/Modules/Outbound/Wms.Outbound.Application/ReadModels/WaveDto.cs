namespace Wms.Outbound.Application.ReadModels;

// Read DTO Wave
public sealed record WaveDto(
    Guid WaveId,
    Guid WarehouseId,
    string Status,
    string? CancelReason,
    IReadOnlyList<Guid> OrderIds,
    int PickingTaskCount,
    int CompletedPickingTaskCount);

// Read DTO ringkas untuk antrean wave per status.
public sealed record WaveListItemDto(Guid WaveId, Guid WarehouseId, string Status, int OrderCount);
