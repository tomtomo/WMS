using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Domain.Enums;
using Wms.Inbound.Domain.UnitTests.TestData;
using Wms.Inbound.Domain.ValueObjects;
using Xunit;

namespace Wms.Inbound.Domain.UnitTests;

// Test perubahan status GoodsReceipt
public sealed class GoodsReceiptStateMachineTests
{
    private static readonly HoldReason _anyReason = HoldReason.Create("alasan berat").Value;

    [Fact]
    public void Scan_after_the_receipt_left_in_progress_is_a_state_conflict()
    {
        var goodsReceipt = GoodsReceiptMother.PendingWithShort();

        var result = goodsReceipt.Scan(GoodsReceiptMother.Scanned(1m));

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("goods_receipt.not_in_progress");
        goodsReceipt.ScannedLines.Should().ContainSingle();
    }

    [Fact]
    public void A_confirmed_receipt_is_terminal_for_every_mutation()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();
        goodsReceipt.Scan(GoodsReceiptMother.Scanned(100m));
        goodsReceipt.CompleteScan();
        goodsReceipt.Confirm();

        goodsReceipt.Scan(GoodsReceiptMother.Scanned(1m)).ErrorType.Should().Be(ResultErrorType.Conflict);
        goodsReceipt.CompleteScan().ErrorType.Should().Be(ResultErrorType.Conflict);
        goodsReceipt.Resolve(Guid.NewGuid(), ResolutionAction.AcceptPartial).ErrorType.Should().Be(ResultErrorType.Conflict);
        goodsReceipt.Confirm().ErrorType.Should().Be(ResultErrorType.Conflict);
        goodsReceipt.Hold(_anyReason).ErrorType.Should().Be(ResultErrorType.Conflict);
        goodsReceipt.Status.Should().Be(GoodsReceiptStatus.Confirmed);
    }

    [Fact]
    public void A_held_receipt_is_terminal_for_every_mutation()
    {
        var goodsReceipt = GoodsReceiptMother.PendingWithShort();
        goodsReceipt.Hold(_anyReason);

        goodsReceipt.Scan(GoodsReceiptMother.Scanned(1m)).ErrorType.Should().Be(ResultErrorType.Conflict);
        goodsReceipt.CompleteScan().ErrorType.Should().Be(ResultErrorType.Conflict);
        goodsReceipt.Resolve(Guid.NewGuid(), ResolutionAction.AcceptPartial).ErrorType.Should().Be(ResultErrorType.Conflict);
        goodsReceipt.Confirm().ErrorType.Should().Be(ResultErrorType.Conflict);
        goodsReceipt.Hold(_anyReason).ErrorType.Should().Be(ResultErrorType.Conflict);
        goodsReceipt.Status.Should().Be(GoodsReceiptStatus.Hold);
    }
}
