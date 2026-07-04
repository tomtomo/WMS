using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Domain.Events;
using Wms.Inventory.Domain.UnitTests.TestData;
using Xunit;

namespace Wms.Inventory.Domain.UnitTests;

public sealed class StockReleaseReservationTests
{
    [Fact]
    public void ReleaseReservation_returns_the_reserved_quantity_to_available_to_promise()
    {
        var stock = StockMother.Available(100m);
        var reservationId = StockMother.NewReservationId();
        stock.Reserve(reservationId, StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(60m));

        var result = stock.ReleaseReservation(reservationId);

        result.IsSuccess.Should().BeTrue();
        stock.AvailableQty.Should().Be(100m);
        stock.DomainEvents.OfType<ReservationReleased>().Should().ContainSingle()
            .Which.ReservationId.Should().Be(reservationId);
    }

    [Fact]
    public void ReleaseReservation_of_an_unknown_reservation_is_not_found()
    {
        var stock = StockMother.Available(100m);

        var result = stock.ReleaseReservation(StockMother.NewReservationId());

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
        result.Error.Code.Should().Be("stock.reservation_not_found");
    }

    [Fact]
    public void Releasing_one_of_several_reservations_only_returns_that_claim()
    {
        var stock = StockMother.Available(100m);
        var first = StockMother.NewReservationId();
        stock.Reserve(first, StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(60m));
        stock.Reserve(StockMother.NewReservationId(), StockMother.WaveId, StockMother.OrderId, StockMother.QtyOf(30m));

        stock.ReleaseReservation(first).IsSuccess.Should().BeTrue();

        stock.AvailableQty.Should().Be(70m);
    }
}
