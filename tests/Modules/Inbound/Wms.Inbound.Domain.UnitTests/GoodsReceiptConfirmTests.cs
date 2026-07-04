using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Domain.Enums;
using Wms.Inbound.Domain.Events;
using Wms.Inbound.Domain.UnitTests.TestData;
using Xunit;

namespace Wms.Inbound.Domain.UnitTests;

// Confirm: semua discrepancy di resolve, susun receivedLines/rejectedLines,
// Pending ke Confirmed, raise GoodsReceiptConfirmed.
public sealed class GoodsReceiptConfirmTests
{
    [Fact]
    public void Confirm_a_clean_receipt_receives_everything_as_good()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(100m, batch: "B-1"));
        goodsReceipt.CompleteScan();

        var result = goodsReceipt.Confirm();

        result.IsSuccess.Should().BeTrue();
        goodsReceipt.Status.Should().Be(GoodsReceiptStatus.Confirmed);
        var received = goodsReceipt.ReceivedLines.Should().ContainSingle().Subject;
        received.Qty.Should().Be(100m);
        received.Batch.Should().Be("B-1");
        received.Status.Should().Be(ReceivedLineStatus.Good);
        goodsReceipt.RejectedLines.Should().BeEmpty();
    }

    [Fact]
    public void Confirm_raises_the_confirmed_event_with_the_outcome_lines()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(100m));
        goodsReceipt.CompleteScan();

        goodsReceipt.Confirm();

        var raised = goodsReceipt.DomainEvents.OfType<GoodsReceiptConfirmed>().Single();
        raised.GoodsReceiptId.Should().Be(goodsReceipt.Id);
        raised.WarehouseId.Should().Be(GoodsReceiptMother.WarehouseId);
        raised.SupplierId.Should().Be(GoodsReceiptMother.SupplierId);
        raised.ReceivedLines.Should().BeEquivalentTo(goodsReceipt.ReceivedLines);
        raised.RejectedLines.Should().BeEmpty();
    }

    [Fact]
    public void Confirm_is_rejected_while_any_discrepancy_is_unresolved()
    {
        var goodsReceipt = GoodsReceiptMother.PendingWithShort();

        var result = goodsReceipt.Confirm();

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("goods_receipt.discrepancy_unresolved");
        goodsReceipt.Status.Should().Be(GoodsReceiptStatus.Pending);
        goodsReceipt.DomainEvents.OfType<GoodsReceiptConfirmed>().Should().BeEmpty();
    }

    [Fact]
    public void Confirm_before_scan_completion_is_a_state_conflict()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();

        var result = goodsReceipt.Confirm();

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("goods_receipt.not_pending");
    }

    [Fact]
    public void Short_delivery_accept_partial_receives_only_what_was_scanned()
    {
        var goodsReceipt = GoodsReceiptMother.PendingWithShort();
        goodsReceipt.Resolve(goodsReceipt.Discrepancies.Single().Id, ResolutionAction.AcceptPartial);

        goodsReceipt.Confirm();

        goodsReceipt.ReceivedLines.Should().ContainSingle().Which.Qty.Should().Be(80m);
        goodsReceipt.RejectedLines.Should().BeEmpty();
    }

    [Fact]
    public void Over_delivery_reject_excess_caps_received_at_the_expected_qty()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(120m));
        goodsReceipt.CompleteScan();
        goodsReceipt.Resolve(goodsReceipt.Discrepancies.Single().Id, ResolutionAction.RejectExcess);

        goodsReceipt.Confirm();

        goodsReceipt.ReceivedLines.Should().ContainSingle().Which.Qty.Should().Be(100m);
        var rejected = goodsReceipt.RejectedLines.Should().ContainSingle().Subject;
        rejected.Qty.Should().Be(20m);
        rejected.Reason.Should().Be(RejectionReason.OverDelivery);
    }

    [Fact]
    public void Qc_hold_send_to_qc_receives_the_line_with_qc_hold_status()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(95m));
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(5m, status: LineStatus.QcHold));
        goodsReceipt.CompleteScan();
        goodsReceipt.Resolve(goodsReceipt.Discrepancies.Single().Id, ResolutionAction.SendToQC);

        goodsReceipt.Confirm();

        goodsReceipt.ReceivedLines.Should().HaveCount(2);
        goodsReceipt.ReceivedLines.Should().ContainSingle(l => l.Status == ReceivedLineStatus.Good && l.Qty == 95m);
        goodsReceipt.ReceivedLines.Should().ContainSingle(l => l.Status == ReceivedLineStatus.QcHold && l.Qty == 5m);
    }

    [Fact]
    public void Wrong_item_return_to_supplier_rejects_the_stray_lines()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(90m));
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(10m, sku: "SKU-STRAY", status: LineStatus.WrongItem));
        goodsReceipt.CompleteScan();
        foreach (var discrepancy in goodsReceipt.Discrepancies)
        {
            var action = discrepancy.Type == DiscrepancyType.ShortDelivery
                ? ResolutionAction.AcceptPartial
                : ResolutionAction.ReturnToSupplier;
            goodsReceipt.Resolve(discrepancy.Id, action);
        }

        goodsReceipt.Confirm();

        goodsReceipt.ReceivedLines.Should().ContainSingle().Which.Qty.Should().Be(90m);
        var rejected = goodsReceipt.RejectedLines.Should().ContainSingle().Subject;
        rejected.Sku.Should().Be("SKU-STRAY");
        rejected.Qty.Should().Be(10m);
        rejected.Reason.Should().Be(RejectionReason.WrongItem);
    }

    [Fact]
    public void Received_lines_keep_the_batch_grain_and_truncate_excess_in_scan_order()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(60m, batch: "B-1"));
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(60m, batch: "B-2"));
        goodsReceipt.CompleteScan();
        goodsReceipt.Resolve(goodsReceipt.Discrepancies.Single().Id, ResolutionAction.RejectExcess);

        goodsReceipt.Confirm();

        goodsReceipt.ReceivedLines.Should().HaveCount(2);
        goodsReceipt.ReceivedLines.Should().ContainSingle(l => l.Batch == "B-1" && l.Qty == 60m);
        goodsReceipt.ReceivedLines.Should().ContainSingle(l => l.Batch == "B-2" && l.Qty == 40m);
        goodsReceipt.RejectedLines.Single().Qty.Should().Be(20m);
    }

    [Fact]
    public void Qc_hold_alone_exceeding_the_cap_is_truncated_and_the_excess_rejected()
    {
        // exp 100, scan 150 QcHold tanpa Good, terima 100 QcHold, tolak 50.
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(150m, status: LineStatus.QcHold));
        goodsReceipt.CompleteScan();
        foreach (var discrepancy in goodsReceipt.Discrepancies)
        {
            var action = discrepancy.Type == DiscrepancyType.OverDelivery
                ? ResolutionAction.RejectExcess
                : ResolutionAction.SendToQC;
            goodsReceipt.Resolve(discrepancy.Id, action);
        }

        goodsReceipt.Confirm().IsSuccess.Should().BeTrue();

        goodsReceipt.ReceivedLines.Should().ContainSingle(
            l => l.Status == ReceivedLineStatus.QcHold && l.Qty == 100m);
        goodsReceipt.RejectedLines.Should().ContainSingle(
            l => l.Reason == RejectionReason.OverDelivery && l.Qty == 50m);
    }

    [Fact]
    public void Confirm_twice_is_a_state_conflict()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(100m));
        goodsReceipt.CompleteScan();
        goodsReceipt.Confirm();

        var result = goodsReceipt.Confirm();

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
    }
}
