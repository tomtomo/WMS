using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Domain.Enums;

namespace Wms.Inbound.Domain.ValueObjects;

// Satu entry scan operator: sku, qty aktual, batch/expiry, plus tag lineStatus (sumbu pertama discrepancy).
public sealed record ScannedLine
{
    private ScannedLine(string sku, decimal actualQty, string? batch, DateOnly? expiry, LineStatus lineStatus)
    {
        Sku = sku;
        ActualQty = actualQty;
        Batch = batch;
        Expiry = expiry;
        LineStatus = lineStatus;
    }

    public string Sku { get; }

    public decimal ActualQty { get; }

    public string? Batch { get; }

    public DateOnly? Expiry { get; }

    public LineStatus LineStatus { get; }

    // Urutan scan, storage tak menjamin urutan load.
    public int ScanSequence { get; internal init; }

    // Blank dinormalisasi ke null agar grouping (sku, batch, expiry) tidak pecah.
    public static Result<ScannedLine> Create(string sku, decimal actualQty, string? batch, DateOnly? expiry, LineStatus lineStatus)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return Result.Invalid<ScannedLine>(new Error("scanned_line.sku_required", "SKU wajib diisi."));
        }

        if (actualQty <= 0)
        {
            return Result.Invalid<ScannedLine>(new Error("scanned_line.qty_invalid", "ActualQty harus lebih dari nol."));
        }

        var normalizedBatch = string.IsNullOrWhiteSpace(batch) ? null : batch.Trim();

        return Result.Success(new ScannedLine(sku.Trim(), actualQty, normalizedBatch, expiry, lineStatus));
    }
}
