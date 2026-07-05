namespace Wms.Outbound.Domain.ValueObjects;

// Satu entry alokasi dari StockAllocationCompleted — reservasi terhadap satu batch stock (Inventory owned by id).
public sealed record AllocationLine(string Sku, Guid ReservationId, decimal AllocatedQty);
