using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Domain.Enums;
using Wms.Inbound.Domain.UnitTests.TestData;
using Wms.Inbound.Domain.ValueObjects;
using Xunit;

namespace Wms.Inbound.Domain.UnitTests;

// Operator scan per carton/line, multi session tanpa mengubah state.
public sealed class GoodsReceiptScanTests
{
    [Fact]
    public void Scan_appends_the_line_and_stays_in_progress()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        var line = ScannedLine.Create(GoodsReceiptMother.Sku, 10m, "BATCH-1", null, LineStatus.Good).Value;

        var result = goodsReceipt.Scan(line);

        result.IsSuccess.Should().BeTrue();
        goodsReceipt.Status.Should().Be(GoodsReceiptStatus.InProgress);
        goodsReceipt.ScannedLines.Should().ContainSingle().Which.Should().Be(line);
    }

    [Fact]
    public void Scan_accumulates_lines_across_sessions()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();

        goodsReceipt.Scan(ScannedLine.Create(GoodsReceiptMother.Sku, 40m, null, null, LineStatus.Good).Value);
        goodsReceipt.Scan(ScannedLine.Create(GoodsReceiptMother.Sku, 55m, null, null, LineStatus.Good).Value);
        goodsReceipt.Scan(ScannedLine.Create(GoodsReceiptMother.Sku, 5m, null, null, LineStatus.QcHold).Value);

        goodsReceipt.ScannedLines.Should().HaveCount(3);
        goodsReceipt.Status.Should().Be(GoodsReceiptStatus.InProgress);
    }

    [Fact]
    public void Scan_assigns_monotonic_scan_sequence()
    {
        // Urutan penerimaan
        var goodsReceipt = GoodsReceiptMother.InProgress();

        goodsReceipt.Scan(ScannedLine.Create(GoodsReceiptMother.Sku, 40m, "B1", null, LineStatus.Good).Value);
        goodsReceipt.Scan(ScannedLine.Create(GoodsReceiptMother.Sku, 55m, "B2", null, LineStatus.Good).Value);
        goodsReceipt.Scan(ScannedLine.Create(GoodsReceiptMother.Sku, 5m, "B3", null, LineStatus.QcHold).Value);

        goodsReceipt.ScannedLines.Select(line => line.ScanSequence).Should().Equal(0, 1, 2);
    }

    [Fact]
    public void Scan_rejects_an_unexpected_sku_tagged_good_as_invalid()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        var strayLine = ScannedLine.Create("SKU-NOT-ON-PO", 10m, null, null, LineStatus.Good).Value;

        var result = goodsReceipt.Scan(strayLine);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("goods_receipt.unexpected_sku");
        goodsReceipt.ScannedLines.Should().BeEmpty();
    }

    [Fact]
    public void Scan_accepts_an_unexpected_sku_tagged_wrong_item()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        var wrongItem = ScannedLine.Create("SKU-NOT-ON-PO", 10m, null, null, LineStatus.WrongItem).Value;

        var result = goodsReceipt.Scan(wrongItem);

        result.IsSuccess.Should().BeTrue();
        goodsReceipt.ScannedLines.Should().ContainSingle();
    }
}
