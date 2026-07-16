namespace Wms.WebUI.Services;

// DTO modul Inbound. Dipisah dari InboundApi supaya file typed-client fokus ke daftar endpoint.
public sealed record GoodsReceiptListItemDto(
    Guid GoodsReceiptId,
    string PoRef,
    Guid SupplierId,
    Guid WarehouseId,
    string DockDoor,
    string Status,
    int DiscrepancyCount,
    DateTimeOffset CreatedAt);

public sealed record CreatedGoodsReceiptResponse(Guid GoodsReceiptId);

public sealed record CreateGoodsReceiptRequest(
    string PoRef,
    Guid SupplierId,
    Guid WarehouseId,
    string DockDoor,
    IReadOnlyList<GrExpectedLineRequest> ExpectedLines);

public sealed record GrExpectedLineRequest(string Sku, decimal ExpectedQty, string Uom);

public sealed record ScanLineRequest(string Sku, decimal ActualQty, string? Batch, DateOnly? Expiry, string LineStatus);

// DTO ringkas untuk detail Goods Receipt, hanya berisi field yang digunakan oleh UI.
public sealed record GoodsReceiptDetailDto(
    Guid GoodsReceiptId,
    string PoRef,
    Guid SupplierId,
    Guid WarehouseId,
    string DockDoor,
    string Status,
    string? HoldReason,
    IReadOnlyList<GrExpectedLineDto> ExpectedLines,
    IReadOnlyList<GrScannedLineDto> ScannedLines,
    IReadOnlyList<GrDiscrepancyDto> Discrepancies);

public sealed record GrExpectedLineDto(string Sku, decimal ExpectedQty, string Uom);

public sealed record GrScannedLineDto(string Sku, decimal ActualQty, string? Batch, string LineStatus);

public sealed record GrDiscrepancyDto(string Sku, string Type, decimal Qty);

// Data attachment GR sesuai ADR-0019 untuk list, upload, dan download.
public sealed record GRAttachmentDto(
    Guid AttachmentId,
    Guid GoodsReceiptId,
    string FileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAt,
    bool IsActive);

public sealed record GRAttachmentUploadedDto(Guid AttachmentId);

public sealed record GRAttachmentDownloadUrl(Uri Url, DateTimeOffset ExpiresAt);
