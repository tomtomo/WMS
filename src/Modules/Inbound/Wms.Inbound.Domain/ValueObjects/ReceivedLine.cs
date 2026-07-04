using Wms.Inbound.Domain.Enums;

namespace Wms.Inbound.Domain.ValueObjects;

// Leaf barang yang benar-benar diterima.
public sealed record ReceivedLine(string Sku, decimal Qty, string? Batch, DateOnly? Expiry, ReceivedLineStatus Status);
