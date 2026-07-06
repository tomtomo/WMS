namespace Wms.Reporting.ReadModels;

// Projection dispatch per (warehouse, period)
public sealed class DispatchSummary
{
    public Guid WarehouseId { get; set; }

    public DateOnly Period { get; set; }

    public decimal DispatchedVolume { get; set; }

    public int WaveThroughput { get; set; }
}
