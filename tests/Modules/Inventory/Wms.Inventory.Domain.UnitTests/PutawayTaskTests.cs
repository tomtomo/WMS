using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Auditing;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Domain.Events;
using Wms.Inventory.Domain.UnitTests.TestData;
using Xunit;

namespace Wms.Inventory.Domain.UnitTests;

// PutawayTask — instruksi pindah balance receiving ke rak.
public sealed class PutawayTaskTests
{
    [Fact]
    public void Create_starts_a_task_in_the_assigned_state_and_announces_it()
    {
        var id = StockMother.NewPutawayTaskId();
        var stockId = StockMother.NewStockId();

        var result = PutawayTask.Create(id, stockId, StockMother.ReceivingLocation, StockMother.RackLocation, StockMother.AssignedTo);

        result.IsSuccess.Should().BeTrue();
        var task = result.Value;
        task.Id.Should().Be(id);
        task.StockId.Should().Be(stockId);
        task.SuggestedDestinationId.Should().Be(StockMother.RackLocation);
        task.AssignedTo.Should().Be(StockMother.AssignedTo);
        task.Status.Should().Be(PutawayStatus.Assigned);
        task.DomainEvents.OfType<PutawayTaskAssigned>().Should().ContainSingle()
            .Which.AssignedTo.Should().Be(StockMother.AssignedTo);
    }

    [Fact]
    public void Create_rejects_an_empty_assignee_as_invalid()
    {
        var result = PutawayTask.Create(
            StockMother.NewPutawayTaskId(), StockMother.NewStockId(), StockMother.ReceivingLocation, StockMother.RackLocation, Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("putaway_task.assignee_required");
    }

    [Fact]
    public void A_task_follows_the_auditable_convention()
    {
        StockMother.AssignedPutaway().Should().BeAssignableTo<IAuditable>();
    }

    [Fact]
    public void Complete_moves_an_assigned_task_to_completed_with_the_actual_destination()
    {
        var task = StockMother.AssignedPutaway();

        var result = task.Complete(StockMother.RackLocation);

        result.IsSuccess.Should().BeTrue();
        task.Status.Should().Be(PutawayStatus.Completed);
        task.ActualDestinationId.Should().Be(StockMother.RackLocation);
        task.DomainEvents.OfType<PutawayTaskCompleted>().Should().ContainSingle();
    }

    [Fact]
    public void Complete_on_an_already_completed_task_is_a_state_conflict()
    {
        var task = StockMother.AssignedPutaway();
        task.Complete(StockMother.RackLocation);

        var result = task.Complete(StockMother.RackLocation);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
        result.Error.Code.Should().Be("putaway_task.not_assigned");
    }
}
