using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Domain.Events;
using Wms.Inbound.Domain.UnitTests.TestData;
using Wms.Inbound.Domain.ValueObjects;
using Xunit;

namespace Wms.Inbound.Domain.UnitTests;

// SPV tolak seluruh GR: Pending ke Hold.
public sealed class GoodsReceiptHoldTests
{
    private static readonly HoldReason _reason = HoldReason.Create("Lot supplier tercampur").Value;

    [Fact]
    public void Hold_moves_the_receipt_to_terminal_hold_with_its_reason()
    {
        var goodsReceipt = GoodsReceiptMother.PendingWithShort();

        var result = goodsReceipt.Hold(_reason);

        result.IsSuccess.Should().BeTrue();
        goodsReceipt.Status.Should().Be(GoodsReceiptStatus.Hold);
        goodsReceipt.HoldReason.Should().Be(_reason);
    }

    [Fact]
    public void Hold_raises_only_the_in_process_held_event_and_never_confirmed()
    {
        var goodsReceipt = GoodsReceiptMother.PendingWithShort();

        goodsReceipt.Hold(_reason);

        var held = goodsReceipt.DomainEvents.OfType<GoodsReceiptHeld>().Single();
        held.GoodsReceiptId.Should().Be(goodsReceipt.Id);
        held.Reason.Should().Be(_reason.Value);
        goodsReceipt.DomainEvents.OfType<GoodsReceiptConfirmed>().Should().BeEmpty();
    }

    [Fact]
    public void Hold_does_not_require_discrepancies_to_be_resolved()
    {
        var goodsReceipt = GoodsReceiptMother.PendingWithShort();
        goodsReceipt.Resolutions.Should().BeEmpty();

        goodsReceipt.Hold(_reason).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Hold_while_still_scanning_is_a_state_conflict()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();

        var result = goodsReceipt.Hold(_reason);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("goods_receipt.not_pending");
    }
}
