using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Domain.ValueObjects;
using Xunit;

namespace Wms.Inbound.Domain.UnitTests;

// Wrapper string istilah domain: DockDoor, HoldReason, ContentRef.
public sealed class SimpleValueObjectTests
{
    [Fact]
    public void DockDoor_create_succeeds_and_trims()
    {
        var result = DockDoor.Create("  DD-01  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("DD-01");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void DockDoor_create_rejects_blank_as_invalid(string value)
    {
        var result = DockDoor.Create(value);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("dock_door.value_required");
    }

    [Fact]
    public void HoldReason_create_succeeds_for_a_meaningful_reason()
    {
        var result = HoldReason.Create("Dokumen surat jalan tidak cocok dengan fisik");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("Dokumen surat jalan tidak cocok dengan fisik");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void HoldReason_create_rejects_blank_as_invalid(string value)
    {
        var result = HoldReason.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("hold_reason.value_required");
    }

    [Fact]
    public void ContentRef_create_succeeds_for_a_blob_path()
    {
        var result = ContentRef.Create("gr-1/att-1/surat-jalan.pdf");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("gr-1/att-1/surat-jalan.pdf");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ContentRef_create_rejects_blank_as_invalid(string value)
    {
        var result = ContentRef.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("content_ref.value_required");
    }
}
