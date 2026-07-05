using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.Domain.UnitTests.TestData;
using Xunit;

namespace Wms.Outbound.Domain.UnitTests;

// order masuk + order diwave.
public sealed class OutboundOrderCreateTests
{
    [Fact]
    public void A_new_order_snapshots_its_lines_as_pending_and_unwaved()
    {
        var order = OutboundOrderMother.New(qty: 10m);

        order.Status.Should().Be(OutboundOrderStatus.New);
        order.WaveId.Should().BeNull();
        order.OrderLines.Should().ContainSingle();
        order.OrderLines[0].AllocationStatus.Should().Be(AllocationStatus.Pending);
        order.OrderLines[0].AllocatedQty.Should().Be(0m);
        order.OrderLines[0].Qty.Should().Be(10m);
    }

    [Fact]
    public void Create_rejects_an_order_with_no_lines()
    {
        var result = OutboundOrder.Create(
            OutboundOrderMother.NewOrderId(), OutboundOrderMother.CustomerId, OutboundOrderMother.DefaultShipTo, []);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("outbound_order.lines_required");
    }

    [Fact]
    public void Create_rejects_an_empty_customer()
    {
        var result = OutboundOrder.Create(
            OutboundOrderMother.NewOrderId(), Guid.Empty, OutboundOrderMother.DefaultShipTo, [OutboundOrderMother.LineOf()]);

        result.Error.Code.Should().Be("outbound_order.customer_required");
    }

    [Fact]
    public void Create_rejects_duplicate_skus_because_allocation_maps_by_sku()
    {
        var result = OutboundOrder.Create(
            OutboundOrderMother.NewOrderId(),
            OutboundOrderMother.CustomerId,
            OutboundOrderMother.DefaultShipTo,
            [OutboundOrderMother.LineOf("SKU-MILK", 10m), OutboundOrderMother.LineOf("SKU-MILK", 5m)]);

        result.Error.Code.Should().Be("outbound_order.sku_duplicated");
    }

    [Fact]
    public void Assigning_a_new_order_to_a_wave_moves_it_in_progress()
    {
        var order = OutboundOrderMother.New();
        var waveId = OutboundOrderMother.NewWaveId();

        var result = order.AssignToWave(waveId);

        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OutboundOrderStatus.InProgress);
        order.WaveId.Should().Be(waveId);
    }

    [Fact]
    public void A_wave_can_only_be_assigned_from_the_new_state()
    {
        var order = OutboundOrderMother.InProgress();

        var result = order.AssignToWave(OutboundOrderMother.NewWaveId());

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("outbound_order.not_new");
    }
}
