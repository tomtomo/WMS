using AwesomeAssertions;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.Domain.UnitTests.TestData;
using Wms.Outbound.Domain.ValueObjects;
using Xunit;

namespace Wms.Outbound.Domain.UnitTests;

public sealed class OutboundScenarioTests
{
    [Fact]
    public void Oo_backlog_an_order_sits_new_until_it_is_waved()
    {
        var order = OutboundOrderMother.New(qty: 10m);

        order.Status.Should().Be(OutboundOrderStatus.New);
        order.WaveId.Should().BeNull();
    }

    [Fact]
    public void Oo_full_a_fully_allocated_order_dispatches_and_closes()
    {
        var order = OutboundOrderMother.New(qty: 10m);
        var waveId = WaveMother.NewWaveId();
        order.AssignToWave(waveId);
        var wave = Wave.Create(waveId, WaveMother.WarehouseId, [order.Id], []).Value;

        var reservationId = Guid.NewGuid();
        order.ApplyAllocation([new AllocationLine("SKU-MILK", reservationId, 10m)], []);

        var task = PickingTaskMother.Assigned(waveId, reservationId, qty: 10m);
        wave.AttachPickingTask(task.Id);
        task.Complete(10m, PickingTaskMother.StagingLocation);
        wave.EvaluateReadiness([task]);
        wave.Status.Should().Be(WaveStatus.Ready);

        wave.Dispatch();
        order.Close();

        wave.Status.Should().Be(WaveStatus.Dispatched);
        order.Status.Should().Be(OutboundOrderStatus.Closed);
    }

    [Fact]
    public void Oo_partial_dispatches_the_allocated_and_returns_the_backorder_to_backlog()
    {
        var order = OutboundOrderMother.New(qty: 10m);
        var waveId = WaveMother.NewWaveId();
        order.AssignToWave(waveId);
        var wave = Wave.Create(waveId, WaveMother.WarehouseId, [order.Id], []).Value;

        var reservationId = Guid.NewGuid();
        order.ApplyAllocation(
            [new AllocationLine("SKU-MILK", reservationId, 8m)],
            [new Shortfall("SKU-MILK", 10m, 8m, 2m)]);
        order.OrderLines.Single().AllocationStatus.Should().Be(AllocationStatus.PartiallyAllocated);

        var task = PickingTaskMother.Assigned(waveId, reservationId, qty: 8m);
        wave.AttachPickingTask(task.Id);
        task.Complete(8m, PickingTaskMother.StagingLocation);
        wave.EvaluateReadiness([task]);
        wave.Dispatch();

        // Backorder outstanding: order kembali ke backlog untuk sisa 2.
        order.ReturnToBacklog("backorder outstanding");

        wave.Status.Should().Be(WaveStatus.Dispatched);
        order.Status.Should().Be(OutboundOrderStatus.New);
        order.WaveId.Should().BeNull();
        var line = order.OrderLines.Should().ContainSingle().Subject;
        line.Qty.Should().Be(2m);
        line.AllocationStatus.Should().Be(AllocationStatus.Pending);
    }

    [Fact]
    public void Oo_unfulfilled_auto_cancels_the_wave_and_returns_the_order_to_backlog()
    {
        var order = OutboundOrderMother.New(qty: 10m);
        var waveId = WaveMother.NewWaveId();
        order.AssignToWave(waveId);
        var wave = Wave.Create(waveId, WaveMother.WarehouseId, [order.Id], []).Value;

        order.ApplyAllocation([], [new Shortfall("SKU-MILK", 10m, 0m, 10m)]);
        order.OrderLines.Single().AllocationStatus.Should().Be(AllocationStatus.Short);

        // Unfulfilled, auto cancel. tanpa PickingTask/dispatch.
        wave.AutoCancel(WaveMother.AnyReason);
        order.ReturnToBacklog("wave nol-terpenuhi");

        wave.Status.Should().Be(WaveStatus.Cancelled);
        order.Status.Should().Be(OutboundOrderStatus.New);
        order.WaveId.Should().BeNull();
        order.OrderLines.Single().AllocationStatus.Should().Be(AllocationStatus.Pending);
        order.OrderLines.Single().Qty.Should().Be(10m);
    }

    [Fact]
    public void An_order_line_preserves_its_uom_snapshot_through_allocation()
    {
        var order = OutboundOrderMother.InProgress(qty: 10m);

        order.ApplyAllocation(
            [new AllocationLine("SKU-MILK", Guid.NewGuid(), 8m)],
            [new Shortfall("SKU-MILK", 10m, 8m, 2m)]);

        order.OrderLines.Single().Uom.Value.Should().Be("CARTON");
        order.OrderLines.Single().Uom.Should().Be(Uom.Create("CARTON").Value);
    }
}
