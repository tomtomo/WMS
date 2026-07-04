namespace Wms.Inventory.Domain.ValueObjects;

// Klaim reservasi Active di dalam boundary Stock
public sealed record ReservationClaim(StockReservationId ReservationId, Guid WaveId, Guid OrderId, decimal Qty);
