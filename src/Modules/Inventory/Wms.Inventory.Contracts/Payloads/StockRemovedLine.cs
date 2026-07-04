namespace Wms.Inventory.Contracts.Payloads;

// Satu leaf stok keluar saat dispatch.
public sealed record StockRemovedLine(
    Guid WarehouseId,
    string Sku,
    string? Batch,
    decimal Qty);
