using Wms.Inbound.Contracts.Enums;

namespace Wms.Inbound.Contracts.Payloads;

// Satu leaf barang yang benar-benar diterima dari GR. Inventory pakai Status untuk pilih OnHand vs Quarantine.
public sealed record ReceivedLine(
    string Sku,
    decimal Qty,
    string? Batch,
    DateOnly? Expiry,
    ReceivedLineStatus Status);
