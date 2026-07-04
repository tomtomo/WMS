using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Domain.UnitTests.TestData;
using Wms.Inventory.Domain.ValueObjects;
using Xunit;

namespace Wms.Inventory.Domain.UnitTests;

public sealed class StockReservationTests
{
    private static readonly ReleaseReason _anyReason = ReleaseReason.Create("wave dibatalkan").Value;

    [Fact]
    public void Create_starts_a_reservation_in_the_active_state()
    {
        var id = StockMother.NewReservationId();
        var stockId = StockMother.NewStockId();

        var result = StockReservation.Create(
            id, stockId, StockMother.WaveId, StockMother.OrderId, StockMother.MilkSku, StockMother.BatchOf(), StockMother.QtyOf(60m));

        result.IsSuccess.Should().BeTrue();
        var reservation = result.Value;
        reservation.Id.Should().Be(id);
        reservation.StockId.Should().Be(stockId);
        reservation.WaveId.Should().Be(StockMother.WaveId);
        reservation.OrderId.Should().Be(StockMother.OrderId);
        reservation.Qty.Should().Be(60m);
        reservation.Status.Should().Be(ReservationStatus.Active);
    }

    [Fact]
    public void Create_rejects_an_empty_wave_as_invalid()
    {
        var result = StockReservation.Create(
            StockMother.NewReservationId(), StockMother.NewStockId(), Guid.Empty, StockMother.OrderId, StockMother.MilkSku, StockMother.BatchOf(), StockMother.QtyOf(60m));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("stock_reservation.wave_required");
    }

    [Fact]
    public void Create_rejects_an_empty_order_as_invalid()
    {
        var result = StockReservation.Create(
            StockMother.NewReservationId(), StockMother.NewStockId(), StockMother.WaveId, Guid.Empty, StockMother.MilkSku, StockMother.BatchOf(), StockMother.QtyOf(60m));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("stock_reservation.order_required");
    }

    [Fact]
    public void A_reservation_follows_the_auditable_convention()
    {
        StockMother.ActiveReservation().Should().BeAssignableTo<IAuditable>();
    }

    [Fact]
    public void Fulfill_moves_an_active_reservation_to_fulfilled()
    {
        var reservation = StockMother.ActiveReservation();

        var result = reservation.Fulfill(StockMother.PickingTaskId);

        result.IsSuccess.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Fulfilled);
        reservation.PickingTaskId.Should().Be(StockMother.PickingTaskId);
    }

    [Fact]
    public void Release_moves_an_active_reservation_to_released_with_a_reason()
    {
        var reservation = StockMother.ActiveReservation();

        var result = reservation.Release(_anyReason);

        result.IsSuccess.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Released);
        reservation.ReleaseReason.Should().Be(_anyReason);
    }

    [Fact]
    public void Fulfill_from_a_released_reservation_is_a_state_conflict()
    {
        var reservation = StockMother.ActiveReservation();
        reservation.Release(_anyReason);

        var result = reservation.Fulfill(StockMother.PickingTaskId);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("stock_reservation.not_active");
    }

    [Fact]
    public void Release_from_a_fulfilled_reservation_is_a_state_conflict()
    {
        var reservation = StockMother.ActiveReservation();
        reservation.Fulfill(StockMother.PickingTaskId);

        var result = reservation.Release(_anyReason);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("stock_reservation.not_active");
    }
}
