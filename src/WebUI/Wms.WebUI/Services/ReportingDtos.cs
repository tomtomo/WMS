namespace Wms.WebUI.Services;

// DTO modul Reporting (baris laporan). Dipisah dari ReportingApi supaya file typed-client fokus ke daftar endpoint.
public sealed record StockOnHandRow(Guid WarehouseId, string Sku, string? Batch, decimal QtyOnHand);

public sealed record DispatchSummaryRow(Guid WarehouseId, DateOnly Period, decimal DispatchedVolume, int WaveThroughput);

public sealed record OperatorProductivityRow(Guid OperatorId, DateOnly Period, int PutawayCount, int PickCount);

public sealed record SupplierPerformanceRow(
    Guid SupplierId,
    DateOnly Period,
    decimal ReceivedQty,
    int ReceiptCount,
    int DiscrepancyCount,
    decimal DiscrepancyRate);
