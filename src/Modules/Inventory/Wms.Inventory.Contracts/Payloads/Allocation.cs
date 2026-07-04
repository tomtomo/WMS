namespace Wms.Inventory.Contracts.Payloads;

// Satu leaf qty terreservasi ke sebuah order-line. Outbound pakai reservationId untuk create PickingTask (idempotent).
public sealed record Allocation(
    Guid OrderId,
    string Sku,
    Guid LocationId,
    string? Batch,
    decimal Qty,
    Guid StockId,
    Guid ReservationId);
