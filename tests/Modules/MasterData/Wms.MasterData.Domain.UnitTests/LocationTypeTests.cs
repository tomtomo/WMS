using AwesomeAssertions;
using Wms.MasterData.Domain.Enums;
using Xunit;

namespace Wms.MasterData.Domain.UnitTests;

// LocationType memetakan ke Stock state (Receiving/Rack/Quarantine/Staging).
public sealed class LocationTypeTests
{
    [Fact]
    public void The_four_core_flow_location_types_are_defined()
    {
        Enum.GetValues<LocationType>().Should().BeEquivalentTo(
        [
            LocationType.ReceivingArea,
            LocationType.Rack,
            LocationType.QuarantineArea,
            LocationType.StagingArea,
        ]);
    }
}
