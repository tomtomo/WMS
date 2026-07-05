using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Domain.Enums;
using Wms.Outbound.Domain.Events;
using Wms.Outbound.Domain.UnitTests.TestData;
using Xunit;

namespace Wms.Outbound.Domain.UnitTests;

public sealed class PickingTaskTests
{
    [Fact]
    public void A_created_task_is_assigned_and_raises_the_assignment_event()
    {
        var waveId = PickingTaskMother.NewWaveId();

        var task = PickingTaskMother.Assigned(waveId, qty: 10m);

        task.Status.Should().Be(PickingTaskStatus.Assigned);
        task.WaveId.Should().Be(waveId);
        task.Qty.Should().Be(10m);
        task.DomainEvents.OfType<PickingTaskAssignedRaised>().Should().ContainSingle();
    }

    [Fact]
    public void Create_rejects_a_non_positive_qty()
    {
        var result = PickingTaskMother.TryCreate(qty: 0m);

        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("picking_task.qty_invalid");
    }

    [Fact]
    public void Create_rejects_an_empty_reservation()
    {
        PickingTaskMother.TryCreate(reservationId: Guid.Empty).Error.Code.Should().Be("picking_task.reservation_required");
    }

    [Fact]
    public void Create_rejects_an_empty_stock()
    {
        PickingTaskMother.TryCreate(stockId: Guid.Empty).Error.Code.Should().Be("picking_task.stock_required");
    }

    [Fact]
    public void Create_rejects_a_blank_sku()
    {
        PickingTaskMother.TryCreate(sku: " ").Error.Code.Should().Be("picking_task.sku_required");
    }

    [Fact]
    public void Create_rejects_an_empty_operator()
    {
        PickingTaskMother.TryCreate(assignedTo: Guid.Empty).Error.Code.Should().Be("picking_task.operator_required");
    }

    [Fact]
    public void Completing_a_task_moves_it_to_completed_and_records_staging()
    {
        var task = PickingTaskMother.Assigned(qty: 10m);

        var result = task.Complete(10m, PickingTaskMother.StagingLocation);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(PickingTaskStatus.Completed);
        task.ActualQty.Should().Be(10m);
        task.StagingLocationId.Should().Be(PickingTaskMother.StagingLocation);
    }

    [Fact]
    public void Complete_rejects_a_qty_that_differs_from_the_assigned_qty()
    {
        var task = PickingTaskMother.Assigned(qty: 10m);

        var result = task.Complete(8m, PickingTaskMother.StagingLocation);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("picking_task.qty_mismatch");
    }

    [Fact]
    public void A_completed_task_is_terminal()
    {
        var task = PickingTaskMother.Completed(qty: 10m);

        var result = task.Complete(10m, PickingTaskMother.StagingLocation);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("picking_task.not_assigned");
    }
}
