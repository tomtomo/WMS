namespace Wms.Outbound.Contracts.Payloads;

// Satu leaf demand order line dalam wave yang dirilis ke Inventory untuk dialokasi.
public sealed record WaveLine(
    Guid OrderId,
    string Sku,
    decimal Qty);
