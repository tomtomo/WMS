namespace Wms.Reporting.ReadModels;

// Read DTO Supplier Performance
public sealed record SupplierPerformanceRow(
    Guid SupplierId,
    DateOnly Period,
    decimal ReceivedQty,
    int ReceiptCount,
    int DiscrepancyCount,
    decimal DiscrepancyRate);
