namespace Wms.Outbound.Application.ReadModels;

// Read DTO OutboundOrder
public sealed record OutboundOrderDto(
    Guid OrderId,
    Guid CustomerId,
    string Status,
    Guid? WaveId,
    IReadOnlyList<OutboundOrderLineDto> Lines);

public sealed record OutboundOrderLineDto(string Sku, decimal Qty, decimal AllocatedQty, string AllocationStatus);
