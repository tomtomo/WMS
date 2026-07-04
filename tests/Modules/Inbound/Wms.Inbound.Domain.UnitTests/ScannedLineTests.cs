using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Domain.Enums;
using Wms.Inbound.Domain.ValueObjects;
using Xunit;

namespace Wms.Inbound.Domain.UnitTests;

// ScannedLine: satu entry scan operator (sku, qty aktual, batch/expiry, tag lineStatus).
public sealed class ScannedLineTests
{
    [Fact]
    public void Create_succeeds_with_batch_and_expiry()
    {
        var expiry = new DateOnly(2026, 12, 31);

        var result = ScannedLine.Create("SKU-MILK", 10m, "BATCH-1", expiry, LineStatus.Good);

        result.IsSuccess.Should().BeTrue();
        result.Value.Sku.Should().Be("SKU-MILK");
        result.Value.ActualQty.Should().Be(10m);
        result.Value.Batch.Should().Be("BATCH-1");
        result.Value.Expiry.Should().Be(expiry);
        result.Value.LineStatus.Should().Be(LineStatus.Good);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_sku_as_invalid(string sku)
    {
        var result = ScannedLine.Create(sku, 10m, null, null, LineStatus.Good);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("scanned_line.sku_required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_rejects_a_non_positive_qty_as_invalid(decimal actualQty)
    {
        var result = ScannedLine.Create("SKU-MILK", actualQty, null, null, LineStatus.Good);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("scanned_line.qty_invalid");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_normalizes_a_blank_batch_to_null(string? batch)
    {
        var result = ScannedLine.Create("SKU-MILK", 10m, batch, null, LineStatus.Good);

        result.IsSuccess.Should().BeTrue();
        result.Value.Batch.Should().BeNull();
    }
}
