using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Domain.Events;
using Wms.Inventory.Domain.UnitTests.TestData;
using Xunit;

namespace Wms.Inventory.Domain.UnitTests;

public sealed class StockReserveTests
{
    [Fact]
    public void An_available_balance_without_reservations_can_promise_its_whole_quantity()
    {
        StockMother.Available(100m).AvailableQty.Should().Be(100m);
    }

    [Fact]
    public void A_non_available_balance_promises_nothing()
    {
        StockMother.OnHand(100m).AvailableQty.Should().Be(0m);
        StockMother.Quarantine(100m).AvailableQty.Should().Be(0m);
    }

    [Fact]
    public void Reserve_lowers_available_to_promise_by_the_reserved_quantity()
    {
        var stock = StockMother.Available(100m);

        var result = stock.Reserve(StockMother.NewReservationId(), StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(60m));

        result.IsSuccess.Should().BeTrue();
        stock.AvailableQty.Should().Be(40m);
        stock.DomainEvents.OfType<StockReserved>().Should().ContainSingle().Which.Qty.Should().Be(60m);
    }

    [Fact]
    public void One_balance_holds_several_concurrent_active_reservations_while_the_sum_stays_within_quantity()
    {
        var stock = StockMother.Available(100m);

        stock.Reserve(StockMother.NewReservationId(), StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(60m))
            .IsSuccess.Should().BeTrue();
        stock.Reserve(StockMother.NewReservationId(), StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(30m))
            .IsSuccess.Should().BeTrue();
        stock.AvailableQty.Should().Be(10m);
    }

    [Fact]
    public void Reserve_beyond_available_to_promise_is_rejected_and_leaves_available_unchanged()
    {
        var stock = StockMother.Available(100m);
        stock.Reserve(StockMother.NewReservationId(), StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(60m));
        stock.Reserve(StockMother.NewReservationId(), StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(30m));

        var result = stock.Reserve(StockMother.NewReservationId(), StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(20m));

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("stock.over_allocate");
        stock.AvailableQty.Should().Be(10m);
        stock.AvailableQty.Should().BeGreaterThanOrEqualTo(0m);
    }

    [Fact]
    public void Reserve_exactly_at_available_to_promise_is_allowed_and_zeroes_it()
    {
        var stock = StockMother.Available(100m);

        stock.Reserve(StockMother.NewReservationId(), StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(100m))
            .IsSuccess.Should().BeTrue();
        stock.AvailableQty.Should().Be(0m);
    }

    [Fact]
    public void Reserve_on_a_balance_that_is_not_available_is_a_state_conflict()
    {
        var stock = StockMother.OnHand(100m);

        var result = stock.Reserve(StockMother.NewReservationId(), StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(10m));

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("stock.not_available");
    }

    [Fact]
    public void Reserve_twice_with_the_same_reservation_is_a_conflict()
    {
        var stock = StockMother.Available(100m);
        var reservationId = StockMother.NewReservationId();
        stock.Reserve(reservationId, StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(10m));

        var result = stock.Reserve(reservationId, StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(10m));

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("stock.reservation_exists");
        stock.AvailableQty.Should().Be(90m);
    }

    [Fact]
    public void Reserve_rejects_an_empty_wave_as_invalid()
    {
        var stock = StockMother.Available(100m);

        var result = stock.Reserve(StockMother.NewReservationId(), Guid.Empty, StockMother.OrderId, StockMother.QtyOf(10m));

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("stock.wave_required");
        stock.AvailableQty.Should().Be(100m);
    }

    [Fact]
    public void Reserve_rejects_an_empty_order_as_invalid()
    {
        var stock = StockMother.Available(100m);

        var result = stock.Reserve(StockMother.NewReservationId(), StockMother.WaveId, Guid.Empty, StockMother.QtyOf(10m));

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("stock.order_required");
    }
}
