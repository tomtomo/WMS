using Wms.BuildingBlocks.Domain.Events;

namespace Wms.Inventory.Domain.Events;

// Klaim stock ditempatkan pada balance Available untuk satu wave.
public sealed record StockReserved(
    StockId StockId,
    StockReservationId ReservationId,
    Guid WaveId,
    Guid OrderId,
    decimal Qty) : IDomainEvent;
