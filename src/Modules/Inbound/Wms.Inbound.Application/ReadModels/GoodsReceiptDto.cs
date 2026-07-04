namespace Wms.Inbound.Application.ReadModels;

// Detail GR untuk view operasional
public sealed record GoodsReceiptDto(
    Guid GoodsReceiptId,
    string PoRef,
    Guid SupplierId,
    Guid WarehouseId,
    string DockDoor,
    string Status,
    string? HoldReason,
    IReadOnlyList<ExpectedLineDto> ExpectedLines,
    IReadOnlyList<ScannedLineDto> ScannedLines,
    IReadOnlyList<QuantityCheckDto> QuantityChecks,
    IReadOnlyList<DiscrepancyDto> Discrepancies,
    IReadOnlyList<ResolutionDto> Resolutions,
    IReadOnlyList<ReceivedLineDto> ReceivedLines,
    IReadOnlyList<RejectedLineDto> RejectedLines);

public sealed record ExpectedLineDto(string Sku, decimal ExpectedQty, string Uom);

public sealed record ScannedLineDto(string Sku, decimal ActualQty, string? Batch, DateOnly? Expiry, string LineStatus);

public sealed record QuantityCheckDto(string Sku, decimal ExpectedQty, decimal ActualQty, string Variance);

public sealed record DiscrepancyDto(Guid DiscrepancyId, string Sku, string Type, decimal Qty);

public sealed record ResolutionDto(Guid DiscrepancyId, string Action, string? Note);

public sealed record ReceivedLineDto(string Sku, decimal Qty, string? Batch, DateOnly? Expiry, string Status);

public sealed record RejectedLineDto(string Sku, decimal Qty, string Reason);
