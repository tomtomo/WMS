namespace Wms.Reporting.ReadModels;

// Read DTO Dispatch Summary per warehouse/periode.
public sealed record DispatchSummaryRow(Guid WarehouseId, DateOnly Period, decimal DispatchedVolume, int WaveThroughput);
