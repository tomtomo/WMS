namespace Wms.WebUI.Services;

// DTO modul Outbound. Dipisah dari OutboundApi supaya file typed-client fokus ke daftar endpoint.
public sealed record OutboundOrderDetailDto(
    Guid OrderId,
    Guid CustomerId,
    string Status,
    Guid? WaveId,
    IReadOnlyList<OutboundOrderLineDto> Lines);

public sealed record OutboundOrderLineDto(string Sku, decimal Qty, decimal AllocatedQty, string AllocationStatus);

public sealed record OrderBacklogDto(Guid OrderId, Guid CustomerId, string Status, IReadOnlyList<OutboundOrderLineDto> Lines);

public sealed record WaveListItemDto(Guid WaveId, Guid WarehouseId, string Status, int OrderCount);

public sealed record PickingTaskDto(Guid PickingTaskId, Guid WaveId, string Sku, decimal Qty, Guid AssignedTo, string Status);

public sealed record CreateOutboundOrderRequest(
    Guid CustomerId,
    string Recipient,
    string AddressLine,
    string City,
    IReadOnlyList<OrderLineRequest> Lines);

public sealed record OrderLineRequest(string Sku, decimal Qty, string Uom);

public sealed record CreateWaveRequest(IReadOnlyList<Guid> OrderIds, Guid WarehouseId);

public sealed record CompletePickingRequest(decimal ActualQty, Guid StagingLocationId, Guid? OperatorId);

public sealed record WaveDetailDto(
    Guid WaveId,
    Guid WarehouseId,
    string Status,
    string? CancelReason,
    IReadOnlyList<Guid> OrderIds,
    int PickingTaskCount,
    int CompletedPickingTaskCount);
