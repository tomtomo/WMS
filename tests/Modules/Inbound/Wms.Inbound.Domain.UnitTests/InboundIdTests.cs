using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.Inbound.Domain.UnitTests;

// Test Typed ID Inbound
public sealed class InboundIdTests
{
    [Fact]
    public void GoodsReceiptId_create_succeeds_for_a_non_empty_guid()
    {
        var value = Guid.NewGuid();

        var result = GoodsReceiptId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void GoodsReceiptId_create_rejects_an_empty_guid_as_invalid()
    {
        var result = GoodsReceiptId.Create(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public void GRAttachmentId_create_succeeds_for_a_non_empty_guid()
    {
        var value = Guid.NewGuid();

        var result = GRAttachmentId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void GRAttachmentId_create_rejects_an_empty_guid_as_invalid()
    {
        var result = GRAttachmentId.Create(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public void Ids_of_the_same_type_and_value_are_equal()
    {
        var value = Guid.NewGuid();

        GoodsReceiptId.Create(value).Value.Should().Be(GoodsReceiptId.Create(value).Value);
    }

    [Fact]
    public void Ids_of_different_types_with_the_same_value_are_not_equal()
    {
        var value = Guid.NewGuid();

        var goodsReceiptId = GoodsReceiptId.Create(value).Value;
        var attachmentId = GRAttachmentId.Create(value).Value;

        goodsReceiptId.Equals(attachmentId).Should().BeFalse();
    }
}
