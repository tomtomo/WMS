using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Inbound.Domain.ValueObjects;

// Snapshot per SKU dari PO saat GR dibuat — dokumen historis stabil terhadap perubahan Product master.
public sealed record ExpectedLine
{
    private ExpectedLine(string sku, decimal expectedQty, string uom)
    {
        Sku = sku;
        ExpectedQty = expectedQty;
        Uom = uom;
    }

    public string Sku { get; }

    public decimal ExpectedQty { get; }

    public string Uom { get; }

    // Sku di trim agar pencocokan expected dan scanned tidak pecah oleh whitespace.
    public static Result<ExpectedLine> Create(string sku, decimal expectedQty, string uom)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return Result.Invalid<ExpectedLine>(new Error("expected_line.sku_required", "SKU wajib diisi."));
        }

        if (expectedQty <= 0)
        {
            return Result.Invalid<ExpectedLine>(new Error("expected_line.qty_invalid", "ExpectedQty harus lebih dari nol."));
        }

        if (string.IsNullOrWhiteSpace(uom))
        {
            return Result.Invalid<ExpectedLine>(new Error("expected_line.uom_required", "UOM wajib diisi."));
        }

        return Result.Success(new ExpectedLine(sku.Trim(), expectedQty, uom.Trim()));
    }
}
