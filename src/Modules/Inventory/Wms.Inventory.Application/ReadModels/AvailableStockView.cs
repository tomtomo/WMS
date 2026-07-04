namespace Wms.Inventory.Application.ReadModels;

// Read DTO balance
public sealed record AvailableStockView(
    Guid StockId,
    string Sku,
    Guid WarehouseId,
    Guid LocationId,
    string Batch,
    DateOnly Expiry,
    decimal Qty,
    decimal AvailableQty,
    string Status);
