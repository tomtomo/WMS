using Wms.Inbound.Domain.Enums;

namespace Wms.Inbound.Domain.ValueObjects;

// Satu penyimpangan yang harus diputuskan SPV
public sealed record Discrepancy(Guid Id, string Sku, DiscrepancyType Type, decimal Qty);
