using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Domain.UnitTests.TestData;
using Wms.Inventory.Domain.ValueObjects;
using Xunit;

namespace Wms.Inventory.Domain.UnitTests;

public sealed class StockStateMachineTests
{
    private static readonly ReleaseReason _anyReason = ReleaseReason.Create("wave dibatalkan").Value;

    [Fact]
    public void A_picked_balance_is_terminal_for_every_stock_mutation()
    {
        var picked = StockMother.Picked();

        picked.PutAway(StockMother.RackLocation).ErrorType.Should().Be(ResultErrorType.Conflict);
        picked.Reserve(StockMother.NewReservationId(), StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(1m))
            .ErrorType.Should().Be(ResultErrorType.Conflict);
        picked.Pick(StockMother.NewReservationId(), StockMother.NewStockId(), StockMother.PickingTaskId, StockMother.StagingLocation)
            .ErrorType.Should().Be(ResultErrorType.Conflict);
        picked.Status.Should().Be(StockStatus.Picked);
    }

    [Fact]
    public void A_quarantine_balance_cannot_be_reserved()
    {
        var result = StockMother.Quarantine().Reserve(
            StockMother.NewReservationId(), StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(10m));

        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("stock.not_available");
    }

    [Fact]
    public void A_fulfilled_reservation_is_terminal()
    {
        var reservation = StockMother.ActiveReservation();
        reservation.Fulfill(StockMother.PickingTaskId);

        reservation.Fulfill(StockMother.PickingTaskId).ErrorType.Should().Be(ResultErrorType.Conflict);
        reservation.Release(_anyReason).ErrorType.Should().Be(ResultErrorType.Conflict);
        reservation.Status.Should().Be(ReservationStatus.Fulfilled);
    }

    [Fact]
    public void A_released_reservation_is_terminal()
    {
        var reservation = StockMother.ActiveReservation();
        reservation.Release(_anyReason);

        reservation.Release(_anyReason).ErrorType.Should().Be(ResultErrorType.Conflict);
        reservation.Fulfill(StockMother.PickingTaskId).ErrorType.Should().Be(ResultErrorType.Conflict);
        reservation.Status.Should().Be(ReservationStatus.Released);
    }
}
