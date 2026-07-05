using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.MasterData.Domain.UnitTests;

// Test Typed ID MasterData: WarehouseId/LocationId (Guid) + Sku
public sealed class MasterDataIdTests
{
    [Fact]
    public void WarehouseId_create_succeeds_for_a_non_empty_guid()
    {
        var value = Guid.NewGuid();

        var result = WarehouseId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void WarehouseId_create_rejects_an_empty_guid_as_invalid()
    {
        var result = WarehouseId.Create(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public void LocationId_create_rejects_an_empty_guid_as_invalid()
    {
        var result = LocationId.Create(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public void Sku_create_succeeds_for_a_non_empty_code()
    {
        var result = Sku.Create("SKU-MILK");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("SKU-MILK");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Sku_create_rejects_empty_or_whitespace_as_invalid(string code)
    {
        var result = Sku.Create(code);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public void Ids_of_the_same_type_and_value_are_equal()
    {
        var value = Guid.NewGuid();

        WarehouseId.Create(value).Value.Should().Be(WarehouseId.Create(value).Value);
    }

    [Fact]
    public void Ids_of_different_types_with_the_same_value_are_not_equal()
    {
        var value = Guid.NewGuid();

        var warehouseId = WarehouseId.Create(value).Value;
        var locationId = LocationId.Create(value).Value;

        warehouseId.Equals(locationId).Should().BeFalse();
    }
}
