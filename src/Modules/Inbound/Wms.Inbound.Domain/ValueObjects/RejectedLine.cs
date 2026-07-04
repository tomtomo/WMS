using Wms.Inbound.Domain.Enums;

namespace Wms.Inbound.Domain.ValueObjects;

// Leaf yang tidak masuk stok.
public sealed record RejectedLine(string Sku, decimal Qty, RejectionReason Reason);
