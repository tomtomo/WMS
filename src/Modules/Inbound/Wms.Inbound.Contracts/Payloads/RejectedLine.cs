using Wms.Inbound.Contracts.Enums;

namespace Wms.Inbound.Contracts.Payloads;

// Leaf yang tak masuk stok (metadata untuk return to vendor).
public sealed record RejectedLine(
    string Sku,
    decimal Qty,
    RejectionReason Reason);
