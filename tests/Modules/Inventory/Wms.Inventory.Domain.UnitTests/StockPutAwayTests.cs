using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Domain.Events;
using Wms.Inventory.Domain.UnitTests.TestData;
using Xunit;

namespace Wms.Inventory.Domain.UnitTests;

// Putaway memindahkan balance OnHand ke rak dan menjadikannya allocatable.
public sealed class StockPutAwayTests
{
    [Fact]
    public void PutAway_moves_an_on_hand_balance_to_a_rack_and_makes_it_available()
    {
        var stock = StockMother.OnHand();

        var result = stock.PutAway(StockMother.RackLocation);

        result.IsSuccess.Should().BeTrue();
        stock.Status.Should().Be(StockStatus.Available);
        stock.LocationId.Should().Be(StockMother.RackLocation);
        stock.DomainEvents.OfType<StockPutAway>().Should().ContainSingle()
            .Which.LocationId.Should().Be(StockMother.RackLocation);
    }

    [Fact]
    public void PutAway_from_quarantine_is_a_state_conflict()
    {
        var stock = StockMother.Quarantine();

        var result = stock.PutAway(StockMother.RackLocation);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("stock.not_on_hand");
        stock.Status.Should().Be(StockStatus.Quarantine);
    }

    [Fact]
    public void PutAway_on_an_already_available_balance_is_a_state_conflict()
    {
        var stock = StockMother.Available();

        var result = stock.PutAway(StockMother.RackLocation);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
    }
}
