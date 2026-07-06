namespace Wms.Reporting.ReadModels;

// Projection penerimaan per (supplier, period)
public sealed class ReceivingSummary
{
    public Guid SupplierId { get; set; }

    public DateOnly Period { get; set; }

    public decimal ReceivedQty { get; set; }

    public int ReceiptCount { get; set; }

    public int DiscrepancyCount { get; set; }
}
