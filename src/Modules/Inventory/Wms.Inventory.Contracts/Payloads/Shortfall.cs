namespace Wms.Inventory.Contracts.Payloads;

// Sisa demand tidak teralokasi satu order-line. Outbound jadikan backorder.
public sealed record Shortfall(
    Guid OrderId,
    string Sku,
    decimal RequestedQty,
    decimal AllocatedQty,
    decimal ShortQty);
