namespace Wms.Reporting.ReadModels;

// Projection on hand fisik per (warehouse, sku, batch)
public sealed class StockOnHandView
{
    public Guid WarehouseId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Batch { get; set; } = string.Empty;

    public decimal QtyOnHand { get; set; }
}
