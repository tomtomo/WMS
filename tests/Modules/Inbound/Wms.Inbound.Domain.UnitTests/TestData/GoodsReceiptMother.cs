using Wms.Inbound.Domain.Enums;
using Wms.Inbound.Domain.ValueObjects;

namespace Wms.Inbound.Domain.UnitTests.TestData;

// Baseline GR valid
internal static class GoodsReceiptMother
{
    public const string Sku = "SKU-MILK";

    public const string PoRef = "PO-2026-001";

    public static readonly Guid SupplierId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid WarehouseId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static GoodsReceiptId NewId() => GoodsReceiptId.Create(Guid.NewGuid()).Value;

    public static DockDoor Dock() => DockDoor.Create("DD-01").Value;

    public static ExpectedLine Expected(string sku = Sku, decimal qty = 100m)
        => ExpectedLine.Create(sku, qty, "carton").Value;

    public static ScannedLine Scanned(
        decimal qty,
        string sku = Sku,
        LineStatus status = LineStatus.Good,
        string? batch = null,
        DateOnly? expiry = null)
        => ScannedLine.Create(sku, qty, batch, expiry, status).Value;

    // GR baru state InProgress. default satu line SKU-MILK expected 100.
    public static GoodsReceipt InProgress(params ExpectedLine[] expectedLines)
    {
        var lines = expectedLines.Length == 0 ? [Expected()] : expectedLines;
        return GoodsReceipt.Create(NewId(), PoRef, SupplierId, WarehouseId, Dock(), lines).Value;
    }

    // GR Pending dengan satu discrepancy ShortDelivery(20) — exp 100, scan 80 Good.
    public static GoodsReceipt PendingWithShort()
    {
        var goodsReceipt = InProgress();
        goodsReceipt.Scan(Scanned(80m));
        goodsReceipt.CompleteScan();
        return goodsReceipt;
    }
}
