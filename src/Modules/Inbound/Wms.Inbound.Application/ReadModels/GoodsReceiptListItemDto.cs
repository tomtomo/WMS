namespace Wms.Inbound.Application.ReadModels;

// Baris antrean review SPV.
public sealed record GoodsReceiptListItemDto(
    Guid GoodsReceiptId,
    string PoRef,
    Guid SupplierId,
    Guid WarehouseId,
    string DockDoor,
    string Status,
    int DiscrepancyCount,
    DateTimeOffset CreatedAt);
