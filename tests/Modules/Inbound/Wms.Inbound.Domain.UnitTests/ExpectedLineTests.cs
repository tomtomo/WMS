using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Domain.ValueObjects;
using Xunit;

namespace Wms.Inbound.Domain.UnitTests;

// ExpectedLine: snapshot per SKU dari PO
public sealed class ExpectedLineTests
{
    [Fact]
    public void Create_succeeds_and_trims_sku_and_uom()
    {
        var result = ExpectedLine.Create("  SKU-MILK  ", 100m, " carton ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Sku.Should().Be("SKU-MILK");
        result.Value.ExpectedQty.Should().Be(100m);
        result.Value.Uom.Should().Be("carton");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_sku_as_invalid(string sku)
    {
        var result = ExpectedLine.Create(sku, 100m, "carton");

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("expected_line.sku_required");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_rejects_a_non_positive_qty_as_invalid(decimal expectedQty)
    {
        var result = ExpectedLine.Create("SKU-MILK", expectedQty, "carton");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expected_line.qty_invalid");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_a_blank_uom_as_invalid(string uom)
    {
        var result = ExpectedLine.Create("SKU-MILK", 100m, uom);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expected_line.uom_required");
    }

    [Fact]
    public void Lines_with_identical_components_are_equal()
    {
        ExpectedLine.Create("SKU-MILK", 100m, "carton").Value
            .Should().Be(ExpectedLine.Create("SKU-MILK", 100m, "carton").Value);
    }
}
