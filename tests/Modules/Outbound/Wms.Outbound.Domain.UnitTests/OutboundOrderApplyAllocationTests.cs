using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.Domain.UnitTests.TestData;
using Wms.Outbound.Domain.ValueObjects;
using Xunit;

namespace Wms.Outbound.Domain.UnitTests;

// reaksi order terhadap outcome alokasi: mapping allocationStatus presisi dan backorder
public sealed class OutboundOrderApplyAllocationTests
{
    [Fact]
    public void A_fully_allocated_line_becomes_allocated_without_backorder()
    {
        var order = OutboundOrderMother.InProgress(qty: 10m);

        var result = order.ApplyAllocation([Alloc("SKU-MILK", 10m)], []);

        result.IsSuccess.Should().BeTrue();
        order.OrderLines.Single().AllocationStatus.Should().Be(AllocationStatus.Allocated);
        order.OrderLines.Single().AllocatedQty.Should().Be(10m);
        order.Backorders.Should().BeEmpty();
    }

    [Fact]
    public void A_partially_allocated_line_becomes_partial_and_records_the_shortfall_as_backorder()
    {
        var order = OutboundOrderMother.InProgress(qty: 10m);

        var result = order.ApplyAllocation([Alloc("SKU-MILK", 8m)], [Short("SKU-MILK", 10m, 8m)]);

        result.IsSuccess.Should().BeTrue();
        order.OrderLines.Single().AllocationStatus.Should().Be(AllocationStatus.PartiallyAllocated);
        order.OrderLines.Single().AllocatedQty.Should().Be(8m);
        order.Backorders.Should().ContainSingle().Which.ShortQty.Should().Be(2m);
    }

    [Fact]
    public void An_unallocated_line_present_in_shortfalls_becomes_short_with_a_full_backorder()
    {
        var order = OutboundOrderMother.InProgress(qty: 10m);

        var result = order.ApplyAllocation([], [Short("SKU-MILK", 10m, 0m)]);

        result.IsSuccess.Should().BeTrue();
        order.OrderLines.Single().AllocationStatus.Should().Be(AllocationStatus.Short);
        order.OrderLines.Single().AllocatedQty.Should().Be(0m);
        order.Backorders.Should().ContainSingle().Which.ShortQty.Should().Be(10m);
    }

    [Fact]
    public void A_line_untouched_by_the_outcome_stays_pending()
    {
        var order = OutboundOrderMother.InProgressWith(
            OutboundOrderMother.LineOf("SKU-MILK", 10m), OutboundOrderMother.LineOf("SKU-BREAD", 5m));

        order.ApplyAllocation([Alloc("SKU-MILK", 10m)], []);

        order.OrderLines.Single(line => line.Sku == "SKU-BREAD").AllocationStatus.Should().Be(AllocationStatus.Pending);
        order.OrderLines.Single(line => line.Sku == "SKU-MILK").AllocationStatus.Should().Be(AllocationStatus.Allocated);
    }

    [Fact]
    public void Allocation_beyond_the_line_demand_violates_the_invariant_and_leaves_the_order_untouched()
    {
        var order = OutboundOrderMother.InProgress(qty: 10m);

        var result = order.ApplyAllocation([Alloc("SKU-MILK", 11m)], []);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("outbound_order.over_allocate");
        order.OrderLines.Single().AllocationStatus.Should().Be(AllocationStatus.Pending);
    }

    [Fact]
    public void Backorders_total_the_sum_of_shortfalls_across_lines()
    {
        var order = OutboundOrderMother.InProgressWith(
            OutboundOrderMother.LineOf("SKU-MILK", 10m), OutboundOrderMother.LineOf("SKU-BREAD", 5m));

        order.ApplyAllocation(
            [Alloc("SKU-MILK", 8m)],
            [Short("SKU-MILK", 10m, 8m), Short("SKU-BREAD", 5m, 0m)]);

        order.Backorders.Sum(backorder => backorder.ShortQty).Should().Be(7m);
    }

    [Fact]
    public void A_fefo_split_across_batches_sums_to_a_full_allocation()
    {
        var order = OutboundOrderMother.InProgress(qty: 10m);

        var result = order.ApplyAllocation([Alloc("SKU-MILK", 6m), Alloc("SKU-MILK", 4m)], []);

        result.IsSuccess.Should().BeTrue();
        order.OrderLines.Single().AllocationStatus.Should().Be(AllocationStatus.Allocated);
        order.OrderLines.Single().AllocatedQty.Should().Be(10m);
    }

    [Fact]
    public void Re_delivering_the_same_outcome_leaves_the_state_unchanged()
    {
        var order = OutboundOrderMother.InProgress(qty: 10m);

        order.ApplyAllocation([Alloc("SKU-MILK", 8m)], [Short("SKU-MILK", 10m, 8m)]);
        order.ApplyAllocation([Alloc("SKU-MILK", 8m)], [Short("SKU-MILK", 10m, 8m)]);

        order.OrderLines.Single().AllocatedQty.Should().Be(8m);
        order.Backorders.Should().ContainSingle().Which.ShortQty.Should().Be(2m);
    }

    [Fact]
    public void Re_applying_a_different_outcome_replaces_prior_state_without_accumulating_backorders()
    {
        var order = OutboundOrderMother.InProgress(qty: 10m);

        // Outcome pertama (reserve 6, short 4), lalu re delivery dengan hasil beda (reserve 9, short 1).
        order.ApplyAllocation([Alloc("SKU-MILK", 6m)], [Short("SKU-MILK", 10m, 6m)]);
        order.ApplyAllocation([Alloc("SKU-MILK", 9m)], [Short("SKU-MILK", 10m, 9m)]);

        // outcome terakhir (9, bukan 6+9), backorder tidak menumpuk (1, bukan 4+1).
        order.OrderLines.Single().AllocatedQty.Should().Be(9m);
        order.OrderLines.Single().AllocationStatus.Should().Be(AllocationStatus.PartiallyAllocated);
        order.Backorders.Should().ContainSingle().Which.ShortQty.Should().Be(1m);
    }

    [Fact]
    public void An_allocation_referencing_a_sku_not_on_the_order_is_rejected()
    {
        var order = OutboundOrderMother.InProgress(qty: 10m);

        var result = order.ApplyAllocation([Alloc("SKU-GHOST", 5m)], []);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("outbound_order.allocation_sku_unknown");
        order.OrderLines.Single().AllocationStatus.Should().Be(AllocationStatus.Pending);
    }

    [Fact]
    public void A_shortfall_referencing_a_sku_not_on_the_order_is_rejected()
    {
        var order = OutboundOrderMother.InProgress(qty: 10m);

        order.ApplyAllocation([], [Short("SKU-GHOST", 5m, 0m)])
            .Error.Code.Should().Be("outbound_order.allocation_sku_unknown");
    }

    [Fact]
    public void Allocation_can_only_be_applied_to_an_in_progress_order()
    {
        var order = OutboundOrderMother.New(qty: 10m);

        var result = order.ApplyAllocation([Alloc("SKU-MILK", 10m)], []);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("outbound_order.not_in_progress");
    }

    private static AllocationLine Alloc(string sku, decimal qty) => new(sku, Guid.NewGuid(), qty);

    private static Shortfall Short(string sku, decimal requested, decimal allocated)
        => new(sku, requested, allocated, requested - allocated);
}
