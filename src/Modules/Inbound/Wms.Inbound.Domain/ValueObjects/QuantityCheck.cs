using Wms.Inbound.Domain.Enums;

namespace Wms.Inbound.Domain.ValueObjects;

// Hasil hitung sistem per expected SKU saat scan selesai (sumbu kedua discrepancy).
public sealed record QuantityCheck
{
    private QuantityCheck(string sku, decimal expectedQty, decimal actualQty, QuantityVariance variance)
    {
        Sku = sku;
        ExpectedQty = expectedQty;
        ActualQty = actualQty;
        Variance = variance;
    }

    public string Sku { get; }

    public decimal ExpectedQty { get; }

    public decimal ActualQty { get; }

    public QuantityVariance Variance { get; }

    // Bukan diterima dari luar, supaya konsisten dengan qty.
    public static QuantityCheck Compute(string sku, decimal expectedQty, decimal actualQty)
    {
        var variance = Math.Sign(actualQty - expectedQty) switch
        {
            0 => QuantityVariance.Normal,
            < 0 => QuantityVariance.ShortDelivery,
            _ => QuantityVariance.OverDelivery,
        };

        return new QuantityCheck(sku, expectedQty, actualQty, variance);
    }
}
