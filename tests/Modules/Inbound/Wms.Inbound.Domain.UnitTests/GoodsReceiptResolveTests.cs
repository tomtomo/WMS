using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Domain.Enums;
using Wms.Inbound.Domain.UnitTests.TestData;
using Xunit;

namespace Wms.Inbound.Domain.UnitTests;

// SPV menentukan Resolution ke tiap discrepancy.
public sealed class GoodsReceiptResolveTests
{
    [Fact]
    public void Resolve_pairs_a_resolution_to_the_discrepancy()
    {
        var goodsReceipt = GoodsReceiptMother.PendingWithShort();
        var discrepancy = goodsReceipt.Discrepancies.Single();

        var result = goodsReceipt.Resolve(discrepancy.Id, ResolutionAction.AcceptPartial, "Sisa 20 tidak dikirim supplier");

        result.IsSuccess.Should().BeTrue();
        var resolution = goodsReceipt.Resolutions.Should().ContainSingle().Subject;
        resolution.DiscrepancyId.Should().Be(discrepancy.Id);
        resolution.Action.Should().Be(ResolutionAction.AcceptPartial);
        resolution.Note.Should().Be("Sisa 20 tidak dikirim supplier");
    }

    [Fact]
    public void Resolve_accepts_a_missing_note()
    {
        var goodsReceipt = GoodsReceiptMother.PendingWithShort();

        var result = goodsReceipt.Resolve(goodsReceipt.Discrepancies.Single().Id, ResolutionAction.AcceptPartial);

        result.IsSuccess.Should().BeTrue();
        goodsReceipt.Resolutions.Single().Note.Should().BeNull();
    }

    [Fact]
    public void Resolve_rejects_an_unknown_discrepancy_as_not_found()
    {
        var goodsReceipt = GoodsReceiptMother.PendingWithShort();

        var result = goodsReceipt.Resolve(Guid.NewGuid(), ResolutionAction.AcceptPartial);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        result.Error.Code.Should().Be("goods_receipt.discrepancy_not_found");
    }

    [Fact]
    public void Resolve_rejects_an_action_that_does_not_match_the_discrepancy_type()
    {
        var goodsReceipt = GoodsReceiptMother.PendingWithShort();

        var result = goodsReceipt.Resolve(goodsReceipt.Discrepancies.Single().Id, ResolutionAction.RejectExcess);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("goods_receipt.resolution_action_mismatch");
        goodsReceipt.Resolutions.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_before_scan_completion_is_a_state_conflict()
    {
        var goodsReceipt = GoodsReceiptMother.InProgress();

        var result = goodsReceipt.Resolve(Guid.NewGuid(), ResolutionAction.AcceptPartial);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("goods_receipt.not_pending");
    }

    [Fact]
    public void Re_resolving_the_same_discrepancy_replaces_the_previous_resolution()
    {
        var goodsReceipt = GoodsReceiptMother.PendingWithShort();
        var discrepancyId = goodsReceipt.Discrepancies.Single().Id;
        goodsReceipt.Resolve(discrepancyId, ResolutionAction.AcceptPartial, "catatan awal");

        var result = goodsReceipt.Resolve(discrepancyId, ResolutionAction.AcceptPartial, "catatan revisi");

        result.IsSuccess.Should().BeTrue();
        goodsReceipt.Resolutions.Should().ContainSingle().Which.Note.Should().Be("catatan revisi");
    }
}
