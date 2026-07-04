using AwesomeAssertions;
using Wms.Inbound.Domain.Enums;
using Wms.Inbound.Domain.Events;
using Wms.Inbound.Domain.UnitTests.TestData;
using Wms.Inbound.Domain.ValueObjects;
using Xunit;

namespace Wms.Inbound.Domain.UnitTests;

// Leaf scenario tree GoodsReceipt
public sealed class GoodsReceiptScenarioTests
{
    [Fact]
    public void GR_OK_clean_receipt_receives_everything()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(100m));
        goodsReceipt.CompleteScan();

        goodsReceipt.Confirm().IsSuccess.Should().BeTrue();

        goodsReceipt.Discrepancies.Should().BeEmpty();
        goodsReceipt.ReceivedLines.Should().ContainSingle(l => l.Qty == 100m && l.Status == ReceivedLineStatus.Good);
        goodsReceipt.RejectedLines.Should().BeEmpty();
        goodsReceipt.DomainEvents.OfType<GoodsReceiptPendingReviewRaised>().Should().ContainSingle();
        goodsReceipt.DomainEvents.OfType<GoodsReceiptConfirmed>().Should().ContainSingle();
        AssertQtyConservation(goodsReceipt);
    }

    [Fact]
    public void GR_SHORT_accept_partial_receives_only_the_delivered_qty()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(80m));
        goodsReceipt.CompleteScan();
        ResolveAll(goodsReceipt);

        goodsReceipt.Confirm().IsSuccess.Should().BeTrue();

        goodsReceipt.ReceivedLines.Should().ContainSingle(l => l.Qty == 80m && l.Status == ReceivedLineStatus.Good);
        goodsReceipt.RejectedLines.Should().BeEmpty();
        AssertQtyConservation(goodsReceipt);
    }

    [Fact]
    public void GR_OVER_reject_excess_caps_at_po_and_alerts_purchasing()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(120m));
        goodsReceipt.CompleteScan();
        ResolveAll(goodsReceipt);

        goodsReceipt.Confirm().IsSuccess.Should().BeTrue();

        goodsReceipt.ReceivedLines.Should().ContainSingle(l => l.Qty == 100m && l.Status == ReceivedLineStatus.Good);
        goodsReceipt.RejectedLines.Should().ContainSingle(l => l.Qty == 20m && l.Reason == RejectionReason.OverDelivery);
        goodsReceipt.DomainEvents.OfType<GoodsReceiptPendingReviewRaised>().Single().HasOverDelivery.Should().BeTrue();
        AssertQtyConservation(goodsReceipt);
    }

    [Fact]
    public void GR_QC_send_to_qc_splits_received_lines_by_status()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(95m));
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(5m, status: LineStatus.QcHold));
        goodsReceipt.CompleteScan();
        ResolveAll(goodsReceipt);

        goodsReceipt.Confirm().IsSuccess.Should().BeTrue();

        goodsReceipt.ReceivedLines.Should().ContainSingle(l => l.Qty == 95m && l.Status == ReceivedLineStatus.Good);
        goodsReceipt.ReceivedLines.Should().ContainSingle(l => l.Qty == 5m && l.Status == ReceivedLineStatus.QcHold);
        goodsReceipt.RejectedLines.Should().BeEmpty();
        AssertQtyConservation(goodsReceipt);
    }

    [Fact]
    public void GR_WRONG_returns_the_stray_sku_and_accepts_the_short_remainder()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(90m));
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(10m, sku: "SKU-STRAY", status: LineStatus.WrongItem));
        goodsReceipt.CompleteScan();
        goodsReceipt.Discrepancies.Should().HaveCount(2);
        ResolveAll(goodsReceipt);

        goodsReceipt.Confirm().IsSuccess.Should().BeTrue();

        goodsReceipt.ReceivedLines.Should().ContainSingle(l => l.Sku == GoodsReceiptMother.Sku && l.Qty == 90m);
        goodsReceipt.RejectedLines.Should().ContainSingle(
            l => l.Sku == "SKU-STRAY" && l.Qty == 10m && l.Reason == RejectionReason.WrongItem);
        AssertQtyConservation(goodsReceipt);
    }

    [Fact]
    public void GR_MIXED_two_axes_on_one_sku_reject_excess_and_send_to_qc()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress(GoodsReceiptMother.Expected(qty: 80m));
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(95m));
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(5m, status: LineStatus.QcHold));
        goodsReceipt.CompleteScan();
        goodsReceipt.Discrepancies.Should().HaveCount(2);
        ResolveAll(goodsReceipt);

        goodsReceipt.Confirm().IsSuccess.Should().BeTrue();

        goodsReceipt.ReceivedLines.Should().ContainSingle(l => l.Qty == 75m && l.Status == ReceivedLineStatus.Good);
        goodsReceipt.ReceivedLines.Should().ContainSingle(l => l.Qty == 5m && l.Status == ReceivedLineStatus.QcHold);
        goodsReceipt.RejectedLines.Should().ContainSingle(l => l.Qty == 20m && l.Reason == RejectionReason.OverDelivery);
        AssertQtyConservation(goodsReceipt);
    }

    [Fact]
    public void GR_HOLD_is_terminal_without_stock_and_without_confirmed_event()
    {
        var goodsReceipt = GoodsReceiptMother.PendingWithShort();

        goodsReceipt.Hold(HoldReason.Create("Perselisihan dengan supplier").Value).IsSuccess.Should().BeTrue();

        goodsReceipt.Status.Should().Be(GoodsReceiptStatus.Hold);
        goodsReceipt.ReceivedLines.Should().BeEmpty();
        goodsReceipt.RejectedLines.Should().BeEmpty();
        goodsReceipt.DomainEvents.OfType<GoodsReceiptPendingReviewRaised>().Should().ContainSingle();
        goodsReceipt.DomainEvents.OfType<GoodsReceiptConfirmed>().Should().BeEmpty();
        goodsReceipt.DomainEvents.OfType<GoodsReceiptHeld>().Should().ContainSingle();
    }

    // SPV memutus tiap discrepancy dengan action default.
    private static void ResolveAll(GoodsReceipt goodsReceipt)
    {
        foreach (var discrepancy in goodsReceipt.Discrepancies)
        {
            var action = discrepancy.Type switch
            {
                DiscrepancyType.ShortDelivery => ResolutionAction.AcceptPartial,
                DiscrepancyType.OverDelivery => ResolutionAction.RejectExcess,
                DiscrepancyType.WrongItem => ResolutionAction.ReturnToSupplier,
                _ => ResolutionAction.SendToQC,
            };

            goodsReceipt.Resolve(discrepancy.Id, action).IsSuccess.Should().BeTrue();
        }
    }

    // received + rejected = total scanned
    private static void AssertQtyConservation(GoodsReceipt goodsReceipt)
    {
        var received = goodsReceipt.ReceivedLines.Sum(line => line.Qty);
        var rejected = goodsReceipt.RejectedLines.Sum(line => line.Qty);
        var scanned = goodsReceipt.ScannedLines.Sum(line => line.ActualQty);

        (received + rejected).Should().Be(scanned);
    }
}
