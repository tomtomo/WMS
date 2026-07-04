using Wms.BuildingBlocks.Domain.Events;

namespace Wms.Inventory.Domain.Events;

// Klaim dilepas — availableQty balik (wave cancel / manual release).
public sealed record ReservationReleased(StockId StockId, StockReservationId ReservationId) : IDomainEvent;
