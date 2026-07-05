namespace Wms.Outbound.Domain.ValueObjects;

// Kekurangan alokasi per SKU dari outcome — demand yang tidak terpenuhi Inventory.
public sealed record Shortfall(string Sku, decimal RequestedQty, decimal AllocatedQty, decimal ShortQty);
