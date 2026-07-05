namespace Wms.Outbound.Domain.ValueObjects;

// Sisa demand tidak teralokasi yang di track agar tidak hilang dan bisa re wave.
public sealed record Backorder(string Sku, decimal ShortQty);
