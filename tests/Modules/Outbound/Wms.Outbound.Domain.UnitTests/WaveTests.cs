using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.Domain.Events;
using Wms.Outbound.Domain.UnitTests.TestData;
using Xunit;

namespace Wms.Outbound.Domain.UnitTests;

public sealed class WaveTests
{
    [Fact]
    public void A_created_wave_is_active_with_its_orders_and_reservation_references()
    {
        var orderId = WaveMother.NewOrderId();
        var reservationId = Guid.NewGuid();

        var wave = Wave.Create(WaveMother.NewWaveId(), WaveMother.WarehouseId, [orderId], [reservationId]).Value;

        wave.Status.Should().Be(WaveStatus.Active);
        wave.OrderIds.Should().ContainSingle().Which.Should().Be(orderId);
        wave.ReservationIds.Should().ContainSingle().Which.Should().Be(reservationId);
        wave.PickingTaskIds.Should().BeEmpty();
    }

    [Fact]
    public void A_created_wave_carries_the_warehouse_it_executes_in()
    {
        var warehouseId = Guid.NewGuid();

        var wave = Wave.Create(WaveMother.NewWaveId(), warehouseId, [WaveMother.NewOrderId()], []).Value;

        wave.WarehouseId.Should().Be(warehouseId);
    }

    [Fact]
    public void Create_rejects_a_wave_without_a_warehouse()
    {
        var result = Wave.Create(WaveMother.NewWaveId(), Guid.Empty, [WaveMother.NewOrderId()], []);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("wave.warehouse_required");
    }

    [Fact]
    public void Create_rejects_a_wave_with_no_orders()
    {
        var result = Wave.Create(WaveMother.NewWaveId(), WaveMother.WarehouseId, [], []);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("wave.orders_required");
    }

    [Fact]
    public void Attaching_the_same_task_twice_is_idempotent()
    {
        var wave = WaveMother.Active();
        var task = PickingTaskMother.Assigned();

        wave.AttachPickingTask(task.Id);
        wave.AttachPickingTask(task.Id);

        wave.PickingTaskIds.Should().ContainSingle();
    }

    [Fact]
    public void Attaching_a_picking_task_is_rejected_after_the_wave_is_cancelled()
    {
        var wave = WaveMother.Active();
        wave.AutoCancel(WaveMother.AnyReason);

        var result = wave.AttachPickingTask(PickingTaskMother.Assigned().Id);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("wave.not_active");
    }

    [Fact]
    public void A_wave_becomes_ready_when_all_its_picking_tasks_are_completed()
    {
        var waveId = WaveMother.NewWaveId();
        var wave = Wave.Create(waveId, WaveMother.WarehouseId, [WaveMother.NewOrderId()], []).Value;
        var task = PickingTaskMother.Completed(waveId);
        wave.AttachPickingTask(task.Id);

        var result = wave.EvaluateReadiness([task]);

        result.IsSuccess.Should().BeTrue();
        wave.Status.Should().Be(WaveStatus.Ready);
        wave.DomainEvents.OfType<WaveReadyRaised>().Should().ContainSingle();
    }

    [Fact]
    public void A_short_line_without_a_picking_task_does_not_block_readiness()
    {
        // Wave dengan 1 task (porsi teralokasi) dan 1 line short yang tidak punya task ke 2.
        var waveId = WaveMother.NewWaveId();
        var wave = Wave.Create(waveId, WaveMother.WarehouseId, [WaveMother.NewOrderId()], []).Value;
        var doneTask = PickingTaskMother.Completed(waveId);
        wave.AttachPickingTask(doneTask.Id);

        wave.EvaluateReadiness([doneTask]);

        wave.Status.Should().Be(WaveStatus.Ready);
    }

    [Fact]
    public void A_wave_stays_active_while_any_picking_task_is_incomplete()
    {
        var waveId = WaveMother.NewWaveId();
        var wave = Wave.Create(waveId, WaveMother.WarehouseId, [WaveMother.NewOrderId()], []).Value;
        var done = PickingTaskMother.Completed(waveId);
        var pending = PickingTaskMother.Assigned(waveId);
        wave.AttachPickingTask(done.Id);
        wave.AttachPickingTask(pending.Id);

        wave.EvaluateReadiness([done, pending]);

        wave.Status.Should().Be(WaveStatus.Active);
        wave.DomainEvents.OfType<WaveReadyRaised>().Should().BeEmpty();
    }

    [Fact]
    public void Auto_cancel_moves_an_active_wave_to_cancelled()
    {
        var wave = WaveMother.Active();

        var result = wave.AutoCancel(WaveMother.AnyReason);

        result.IsSuccess.Should().BeTrue();
        wave.Status.Should().Be(WaveStatus.Cancelled);
        wave.CancelReason.Should().Be(WaveMother.AnyReason);
        wave.DomainEvents.OfType<WaveCancelledRaised>().Should().ContainSingle();
    }

    [Fact]
    public void Dispatch_moves_a_ready_wave_to_dispatched()
    {
        var waveId = WaveMother.NewWaveId();
        var wave = Wave.Create(waveId, WaveMother.WarehouseId, [WaveMother.NewOrderId()], []).Value;
        var task = PickingTaskMother.Completed(waveId);
        wave.AttachPickingTask(task.Id);
        wave.EvaluateReadiness([task]);

        var result = wave.Dispatch();

        result.IsSuccess.Should().BeTrue();
        wave.Status.Should().Be(WaveStatus.Dispatched);
        wave.DomainEvents.OfType<WaveDispatchedRaised>().Should().ContainSingle();
    }

    [Fact]
    public void Dispatch_is_rejected_before_the_wave_is_ready()
    {
        var wave = WaveMother.Active();

        var result = wave.Dispatch();

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("wave.not_ready");
    }
}
