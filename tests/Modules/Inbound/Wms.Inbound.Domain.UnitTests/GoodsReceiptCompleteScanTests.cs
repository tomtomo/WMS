using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Domain.Enums;
using Wms.Inbound.Domain.Events;
using Wms.Inbound.Domain.UnitTests.TestData;
using Xunit;

namespace Wms.Inbound.Domain.UnitTests;

// Declare scan selesai: hitung quantityChecks per expected SKU, discrepancies dua sumbu,
// InProgress ke Pending, raise GoodsReceiptPendingReviewRaised.
public sealed class GoodsReceiptCompleteScanTests
{
    [Fact]
    public void Complete_scan_with_a_clean_receipt_yields_pending_without_discrepancy()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(100m));

        var result = goodsReceipt.CompleteScan();

        result.IsSuccess.Should().BeTrue();
        goodsReceipt.Status.Should().Be(GoodsReceiptStatus.Pending);
        goodsReceipt.QuantityChecks.Should().ContainSingle()
            .Which.Variance.Should().Be(QuantityVariance.Normal);
        goodsReceipt.Discrepancies.Should().BeEmpty();
    }

    [Fact]
    public void Complete_scan_always_raises_pending_review_even_without_discrepancy()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(100m));

        goodsReceipt.CompleteScan();

        var raised = goodsReceipt.DomainEvents.OfType<GoodsReceiptPendingReviewRaised>().Single();
        raised.GoodsReceiptId.Should().Be(goodsReceipt.Id);
        raised.WarehouseId.Should().Be(GoodsReceiptMother.WarehouseId);
        raised.HasOverDelivery.Should().BeFalse();
        raised.DiscrepancyCount.Should().Be(0);
    }

    [Fact]
    public void Short_scan_compiles_a_short_delivery_discrepancy_with_the_missing_qty()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(80m));

        goodsReceipt.CompleteScan();

        var discrepancy = goodsReceipt.Discrepancies.Should().ContainSingle().Subject;
        discrepancy.Type.Should().Be(DiscrepancyType.ShortDelivery);
        discrepancy.Sku.Should().Be(GoodsReceiptMother.Sku);
        discrepancy.Qty.Should().Be(20m);
    }

    [Fact]
    public void Over_scan_compiles_an_over_delivery_discrepancy_and_flags_the_event()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(120m));

        goodsReceipt.CompleteScan();

        var discrepancy = goodsReceipt.Discrepancies.Should().ContainSingle().Subject;
        discrepancy.Type.Should().Be(DiscrepancyType.OverDelivery);
        discrepancy.Qty.Should().Be(20m);
        goodsReceipt.DomainEvents.OfType<GoodsReceiptPendingReviewRaised>().Single()
            .HasOverDelivery.Should().BeTrue();
    }

    [Fact]
    public void Qc_hold_lines_count_toward_quantity_so_variance_stays_normal()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(95m));
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(5m, status: LineStatus.QcHold));

        goodsReceipt.CompleteScan();

        goodsReceipt.QuantityChecks.Should().ContainSingle()
            .Which.Variance.Should().Be(QuantityVariance.Normal);
        var discrepancy = goodsReceipt.Discrepancies.Should().ContainSingle().Subject;
        discrepancy.Type.Should().Be(DiscrepancyType.QcHold);
        discrepancy.Qty.Should().Be(5m);
    }

    [Fact]
    public void One_sku_hit_on_both_axes_yields_two_separate_discrepancies()
    {
        // expected 80, scan 100 (95 Good + 5 QcHold), OverDelivery(20), QcHold(5).
        var goodsReceipt = GoodsReceiptMother.InProgress(GoodsReceiptMother.Expected(qty: 80m));
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(95m));
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(5m, status: LineStatus.QcHold));

        goodsReceipt.CompleteScan();

        goodsReceipt.Discrepancies.Should().HaveCount(2);
        goodsReceipt.Discrepancies.Should().ContainSingle(d => d.Type == DiscrepancyType.OverDelivery && d.Qty == 20m);
        goodsReceipt.Discrepancies.Should().ContainSingle(d => d.Type == DiscrepancyType.QcHold && d.Qty == 5m);
        goodsReceipt.DomainEvents.OfType<GoodsReceiptPendingReviewRaised>().Single()
            .DiscrepancyCount.Should().Be(2);
    }

    [Fact]
    public void Wrong_item_lines_group_into_one_discrepancy_per_stray_sku()
    {
        // scan 90 MILK Good + 10 wrong-SKU, Short(10) di MILK + WrongItem(10)
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(90m));
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(4m, sku: "SKU-STRAY", status: LineStatus.WrongItem));
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(6m, sku: "SKU-STRAY", status: LineStatus.WrongItem));

        goodsReceipt.CompleteScan();

        goodsReceipt.Discrepancies.Should().HaveCount(2);
        goodsReceipt.Discrepancies.Should().ContainSingle(
            d => d.Type == DiscrepancyType.ShortDelivery && d.Sku == GoodsReceiptMother.Sku && d.Qty == 10m);
        goodsReceipt.Discrepancies.Should().ContainSingle(
            d => d.Type == DiscrepancyType.WrongItem && d.Sku == "SKU-STRAY" && d.Qty == 10m);
    }

    [Fact]
    public void Wrong_item_lines_do_not_count_toward_the_expected_skus_quantity()
    {
        // Label SKU-MILK tapi isi bukan pesanan: bukan stok MILK, jadi MILK tetap Short.
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(90m));
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(10m, status: LineStatus.WrongItem));

        goodsReceipt.CompleteScan();

        goodsReceipt.QuantityChecks.Single().Variance.Should().Be(QuantityVariance.ShortDelivery);
        goodsReceipt.Discrepancies.Should().HaveCount(2);
        goodsReceipt.Discrepancies.Should().ContainSingle(d => d.Type == DiscrepancyType.ShortDelivery && d.Qty == 10m);
        goodsReceipt.Discrepancies.Should().ContainSingle(d => d.Type == DiscrepancyType.WrongItem && d.Qty == 10m);
    }

    [Fact]
    public void An_expected_sku_that_was_never_scanned_is_fully_short()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress(
            GoodsReceiptMother.Expected(),
            GoodsReceiptMother.Expected(sku: "SKU-CHEESE", qty: 50m));
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(100m));

        goodsReceipt.CompleteScan();

        goodsReceipt.Discrepancies.Should().ContainSingle(
            d => d.Type == DiscrepancyType.ShortDelivery && d.Sku == "SKU-CHEESE" && d.Qty == 50m);
    }

    [Fact]
    public void Complete_scan_twice_is_a_state_conflict()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(100m));
        goodsReceipt.CompleteScan();

        var result = goodsReceipt.CompleteScan();

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("goods_receipt.not_in_progress");
    }
}
