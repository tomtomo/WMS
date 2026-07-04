using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.Inventory.Domain.UnitTests;

// Test Typed ID Inventory
public sealed class InventoryIdTests
{
    [Fact]
    public void StockId_create_succeeds_for_a_non_empty_guid()
    {
        var value = Guid.NewGuid();

        var result = StockId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void StockId_create_rejects_an_empty_guid_as_invalid()
    {
        var result = StockId.Create(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public void StockReservationId_create_succeeds_for_a_non_empty_guid()
    {
        var value = Guid.NewGuid();

        var result = StockReservationId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void StockReservationId_create_rejects_an_empty_guid_as_invalid()
    {
        var result = StockReservationId.Create(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public void PutawayTaskId_create_succeeds_for_a_non_empty_guid()
    {
        var value = Guid.NewGuid();

        var result = PutawayTaskId.Create(value);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(value);
    }

    [Fact]
    public void PutawayTaskId_create_rejects_an_empty_guid_as_invalid()
    {
        var result = PutawayTaskId.Create(Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public void Ids_of_the_same_type_and_value_are_equal()
    {
        var value = Guid.NewGuid();

        StockId.Create(value).Value.Should().Be(StockId.Create(value).Value);
    }

    [Fact]
    public void Ids_of_different_types_with_the_same_value_are_not_equal()
    {
        var value = Guid.NewGuid();

        var stockId = StockId.Create(value).Value;
        var reservationId = StockReservationId.Create(value).Value;

        stockId.Equals(reservationId).Should().BeFalse();
    }
}
