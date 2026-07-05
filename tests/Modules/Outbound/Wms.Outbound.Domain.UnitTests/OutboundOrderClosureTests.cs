using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.Domain.Events;
using Wms.Outbound.Domain.UnitTests.TestData;
using Wms.Outbound.Domain.ValueObjects;
using Xunit;

namespace Wms.Outbound.Domain.UnitTests;

// Return to backlog
public sealed class OutboundOrderClosureTests
{
    [Fact]
    public void Return_to_backlog_resets_an_order_to_new_and_clears_the_wave()
    {
        var order = OutboundOrderMother.InProgress(qty: 10m);

        var result = order.ReturnToBacklog("wave nol-terpenuhi");

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OutboundOrderStatus.New);
        order.WaveId.Should().BeNull();
        order.OrderLines.Should().OnlyContain(line => line.AllocationStatus == AllocationStatus.Pending);
        order.Backorders.Should().BeEmpty();
        order.DomainEvents.OfType<OrderReturnedToBacklogRaised>().Should().ContainSingle();
    }

    [Fact]
    public void Return_to_backlog_after_a_partial_allocation_keeps_only_the_outstanding_backorder_qty()
    {
        var order = OutboundOrderMother.PartiallyAllocated(qty: 10m, allocated: 8m);

        order.ReturnToBacklog("backorder outstanding");

        var line = order.OrderLines.Should().ContainSingle().Subject;
        line.Qty.Should().Be(2m);
        line.AllocatedQty.Should().Be(0m);
        line.AllocationStatus.Should().Be(AllocationStatus.Pending);
    }

    [Fact]
    public void Return_to_backlog_drops_lines_that_were_fully_allocated_and_shipped()
    {
        var order = OutboundOrderMother.InProgressWith(
            OutboundOrderMother.LineOf("SKU-MILK", 10m), OutboundOrderMother.LineOf("SKU-BREAD", 5m));
        order.ApplyAllocation(
            [new AllocationLine("SKU-MILK", Guid.NewGuid(), 10m)],
            [new Shortfall("SKU-BREAD", 5m, 0m, 5m)]);

        order.ReturnToBacklog("partial dispatch");

        var line = order.OrderLines.Should().ContainSingle().Subject;
        line.Sku.Should().Be("SKU-BREAD");
        line.Qty.Should().Be(5m);
    }

    [Fact]
    public void Return_to_backlog_is_only_valid_from_in_progress()
    {
        var order = OutboundOrderMother.New();

        var result = order.ReturnToBacklog("x");

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("outbound_order.not_in_progress");
    }

    [Fact]
    public void A_fully_fulfilled_order_can_be_closed()
    {
        var order = OutboundOrderMother.FullyAllocated(qty: 10m);

        var result = order.Close();

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OutboundOrderStatus.Closed);
        order.DomainEvents.OfType<OutboundOrderClosedRaised>().Should().ContainSingle();
    }

    [Fact]
    public void An_order_with_a_short_line_cannot_be_closed()
    {
        var order = OutboundOrderMother.InProgress(qty: 10m);
        order.ApplyAllocation([], [new Shortfall("SKU-MILK", 10m, 0m, 10m)]);

        var result = order.Close();

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("outbound_order.not_fully_fulfilled");
    }

    [Fact]
    public void An_order_with_a_partial_line_cannot_be_closed()
    {
        var order = OutboundOrderMother.PartiallyAllocated(qty: 10m, allocated: 8m);

        order.Close().Error.Code.Should().Be("outbound_order.not_fully_fulfilled");
    }

    [Fact]
    public void Close_is_only_valid_from_in_progress()
    {
        var order = OutboundOrderMother.New();

        var result = order.Close();

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("outbound_order.not_in_progress");
    }
}
