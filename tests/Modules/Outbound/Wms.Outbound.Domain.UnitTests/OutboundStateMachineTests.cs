using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.Domain.UnitTests.TestData;
using Wms.Outbound.Domain.ValueObjects;
using Xunit;

namespace Wms.Outbound.Domain.UnitTests;

public sealed class OutboundStateMachineTests
{
    [Fact]
    public void A_closed_order_rejects_every_further_mutation()
    {
        var order = OutboundOrderMother.FullyAllocated(qty: 10m);
        order.Close();

        order.AssignToWave(WaveMother.NewWaveId()).ErrorType.Should().Be(ResultErrorType.Conflict);
        order.ApplyAllocation([new AllocationLine("SKU-MILK", Guid.NewGuid(), 10m)], []).ErrorType.Should().Be(ResultErrorType.Conflict);
        order.ReturnToBacklog("x").ErrorType.Should().Be(ResultErrorType.Conflict);
        order.Close().ErrorType.Should().Be(ResultErrorType.Conflict);
        order.Status.Should().Be(OutboundOrderStatus.Closed);
    }

    [Fact]
    public void A_dispatched_wave_rejects_every_further_mutation()
    {
        var waveId = WaveMother.NewWaveId();
        var wave = Wave.Create(waveId, [WaveMother.NewOrderId()], []).Value;
        var task = PickingTaskMother.Completed(waveId);
        wave.AttachPickingTask(task.Id);
        wave.EvaluateReadiness([task]);
        wave.Dispatch();

        wave.AttachPickingTask(PickingTaskMother.Assigned().Id).ErrorType.Should().Be(ResultErrorType.Conflict);
        wave.EvaluateReadiness([task]).ErrorType.Should().Be(ResultErrorType.Conflict);
        wave.AutoCancel(WaveMother.AnyReason).ErrorType.Should().Be(ResultErrorType.Conflict);
        wave.Dispatch().ErrorType.Should().Be(ResultErrorType.Conflict);
        wave.Status.Should().Be(WaveStatus.Dispatched);
    }

    [Fact]
    public void A_cancelled_wave_rejects_every_further_mutation()
    {
        var wave = WaveMother.Active();
        wave.AutoCancel(WaveMother.AnyReason);

        wave.AutoCancel(WaveMother.AnyReason).ErrorType.Should().Be(ResultErrorType.Conflict);
        wave.Dispatch().ErrorType.Should().Be(ResultErrorType.Conflict);
        wave.Status.Should().Be(WaveStatus.Cancelled);
    }

    [Fact]
    public void A_completed_picking_task_rejects_re_completion()
    {
        var task = PickingTaskMother.Completed(qty: 10m);

        task.Complete(10m, PickingTaskMother.StagingLocation).ErrorType.Should().Be(ResultErrorType.Conflict);
        task.Status.Should().Be(PickingTaskStatus.Completed);
    }
}
