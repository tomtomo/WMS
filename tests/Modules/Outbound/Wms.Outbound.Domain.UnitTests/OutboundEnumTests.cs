using AwesomeAssertions;
using Wms.Outbound.Domain.Enums;
using Xunit;

namespace Wms.Outbound.Domain.UnitTests;

public sealed class OutboundEnumTests
{
    [Fact]
    public void Outbound_order_status_defines_its_lifecycle_states()
    {
        Enum.GetNames<OutboundOrderStatus>().Should().Equal("New", "InProgress", "Closed");
    }

    [Fact]
    public void Allocation_status_defines_the_four_precise_line_outcomes()
    {
        Enum.GetNames<AllocationStatus>().Should().Equal("Pending", "Allocated", "PartiallyAllocated", "Short");
    }

    [Fact]
    public void Wave_status_defines_its_lifecycle_states()
    {
        Enum.GetNames<WaveStatus>().Should().Equal("Active", "Ready", "Dispatched", "Cancelled");
    }

    [Fact]
    public void Picking_task_status_defines_its_lifecycle_states()
    {
        Enum.GetNames<PickingTaskStatus>().Should().Equal("Assigned", "Completed");
    }
}
