using AwesomeAssertions;
using Wms.Inventory.Domain.Enums;
using Xunit;

namespace Wms.Inventory.Domain.UnitTests;

// State enum
public sealed class StockEnumTests
{
    [Fact]
    public void StockStatus_has_exactly_the_four_physical_states()
    {
        Enum.GetNames<StockStatus>().Should().BeEquivalentTo("Quarantine", "OnHand", "Available", "Picked");
    }

    [Fact]
    public void ReservationStatus_has_exactly_the_three_lifecycle_states()
    {
        Enum.GetNames<ReservationStatus>().Should().BeEquivalentTo("Active", "Fulfilled", "Released");
    }

    [Fact]
    public void PutawayStatus_has_exactly_the_two_task_states()
    {
        Enum.GetNames<PutawayStatus>().Should().BeEquivalentTo("Assigned", "Completed");
    }
}
