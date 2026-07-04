using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Domain.Events;
using Wms.Inventory.Domain.UnitTests.TestData;
using Xunit;

namespace Wms.Inventory.Domain.UnitTests;

// Picking = split balance fisik: qty terreservasi pindah ke balance Picked baru.
public sealed class StockPickTests
{
    [Fact]
    public void Pick_splits_the_reserved_quantity_into_a_new_picked_balance_conserving_quantity()
    {
        var stock = StockMother.Available(100m);
        var reservationId = StockMother.NewReservationId();
        stock.Reserve(reservationId, StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(60m));
        var pickedId = StockMother.NewStockId();

        var result = stock.Pick(reservationId, pickedId, StockMother.PickingTaskId, StockMother.StagingLocation);

        result.IsSuccess.Should().BeTrue();
        var picked = result.Value;
        picked.Id.Should().Be(pickedId);
        picked.Status.Should().Be(StockStatus.Picked);
        picked.Qty.Should().Be(60m);
        picked.LocationId.Should().Be(StockMother.StagingLocation);
        stock.Qty.Should().Be(40m);
        (100m - stock.Qty).Should().Be(picked.Qty);
        stock.DomainEvents.OfType<StockPicked>().Should().ContainSingle().Which.Qty.Should().Be(60m);
    }

    [Fact]
    public void Pick_carries_the_picking_task_and_wave_onto_the_picked_balance()
    {
        var stock = StockMother.Available(100m);
        var reservationId = StockMother.NewReservationId();
        stock.Reserve(reservationId, StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(60m));

        var picked = stock.Pick(reservationId, StockMother.NewStockId(), StockMother.PickingTaskId, StockMother.StagingLocation).Value;

        picked.PickingTaskId.Should().Be(StockMother.PickingTaskId);
        picked.WaveId.Should().Be(StockMother.WaveId);
    }

    [Fact]
    public void Pick_settles_only_the_picked_claim_and_leaves_the_others_holding_available()
    {
        var stock = StockMother.Available(100m);
        var picked = StockMother.NewReservationId();
        stock.Reserve(picked, StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(60m));
        stock.Reserve(StockMother.NewReservationId(), StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(20m));

        stock.Pick(picked, StockMother.NewStockId(), StockMother.PickingTaskId, StockMother.StagingLocation);

        stock.Qty.Should().Be(40m);
        stock.AvailableQty.Should().Be(20m);
    }

    [Fact]
    public void Pick_on_a_balance_that_is_not_available_is_a_state_conflict()
    {
        var stock = StockMother.OnHand(100m);

        var result = stock.Pick(StockMother.NewReservationId(), StockMother.NewStockId(), StockMother.PickingTaskId, StockMother.StagingLocation);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("stock.not_available");
    }

    [Fact]
    public void Pick_of_an_unknown_reservation_is_not_found()
    {
        var stock = StockMother.Available(100m);

        var result = stock.Pick(StockMother.NewReservationId(), StockMother.NewStockId(), StockMother.PickingTaskId, StockMother.StagingLocation);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        result.Error.Code.Should().Be("stock.reservation_not_found");
    }
}
