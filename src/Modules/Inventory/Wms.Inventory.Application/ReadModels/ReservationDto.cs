namespace Wms.Inventory.Application.ReadModels;

// Read DTO reservasi (klaim kuantitas) per wave.
public sealed record ReservationDto(
    Guid ReservationId,
    Guid StockId,
    Guid WaveId,
    Guid OrderId,
    string Sku,
    string Batch,
    decimal Qty,
    string Status,
    Guid? PickingTaskId);
