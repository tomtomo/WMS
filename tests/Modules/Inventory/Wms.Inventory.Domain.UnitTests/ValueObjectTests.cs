using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Domain.ValueObjects;
using Xunit;

namespace Wms.Inventory.Domain.UnitTests;

// Validasi value object Inventory
public sealed class ValueObjectTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Sku_rejects_blank_as_invalid(string value)
    {
        var result = Sku.Create(value);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("sku.value_required");
    }

    [Fact]
    public void Sku_trims_surrounding_whitespace()
    {
        Sku.Create("  SKU-MILK  ").Value.Value.Should().Be("SKU-MILK");
    }

    [Fact]
    public void LocationId_rejects_an_empty_guid_as_invalid()
    {
        var result = LocationId.Create(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("location_id.value_required");
    }

    [Fact]
    public void LocationId_carries_a_non_empty_guid()
    {
        var value = Guid.NewGuid();

        LocationId.Create(value).Value.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Batch_rejects_blank_as_invalid(string value)
    {
        var result = Batch.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("batch.value_required");
    }

    [Fact]
    public void Batch_trims_surrounding_whitespace()
    {
        Batch.Create("  LOT-01  ").Value.Value.Should().Be("LOT-01");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Quantity_rejects_a_non_positive_value_as_invalid(int value)
    {
        var result = Quantity.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("quantity.must_be_positive");
    }

    [Fact]
    public void Quantity_accepts_a_positive_decimal()
    {
        Quantity.Create(12.5m).Value.Value.Should().Be(12.5m);
    }

    [Fact]
    public void Expiry_rejects_the_default_date_as_invalid()
    {
        var result = Expiry.Create(default);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("expiry.value_required");
    }

    [Fact]
    public void Expiry_carries_the_given_date()
    {
        var date = new DateOnly(2026, 12, 31);

        Expiry.Create(date).Value.Value.Should().Be(date);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ReleaseReason_rejects_blank_as_invalid(string value)
    {
        var result = ReleaseReason.Create(value);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("release_reason.value_required");
    }

    [Fact]
    public void ReleaseReason_trims_surrounding_whitespace()
    {
        ReleaseReason.Create("  wave dibatalkan  ").Value.Value.Should().Be("wave dibatalkan");
    }
}
