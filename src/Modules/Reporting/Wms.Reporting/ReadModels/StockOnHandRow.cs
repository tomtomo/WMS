namespace Wms.Reporting.ReadModels;

// Read DTO Stock on Hand per SKU
public sealed record StockOnHandRow(Guid WarehouseId, string Sku, string? Batch, decimal QtyOnHand);
