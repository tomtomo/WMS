using AwesomeAssertions;
using MediatR;
using NSubstitute;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Behaviors;
using Wms.BuildingBlocks.Application.UnitTests.TestDoubles;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.BuildingBlocks.Application.UnitTests;

// Test TransactionBehavior
public sealed class TransactionBehaviorTests
{
    [Fact]
    public async Task Command_success_commits_exactly_once()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        var behavior = new TransactionBehavior<DoubleValueCommand, Result<int>>(unitOfWork);
        RequestHandlerDelegate<Result<int>> next = _ => Task.FromResult(Result.Success(42));

        var result = await behavior.Handle(new DoubleValueCommand(21), next, CancellationToken.None);

        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task Command_failure_does_not_commit()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var behavior = new TransactionBehavior<DoubleValueCommand, Result<int>>(unitOfWork);
        RequestHandlerDelegate<Result<int>> next =
            _ => Task.FromResult(Result.Failure<int>(new Error("inventory.rejected", "ditolak")));

        var result = await behavior.Handle(new DoubleValueCommand(21), next, CancellationToken.None);

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Query_bypasses_the_transaction()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        var behavior = new TransactionBehavior<GetValueQuery, Result<int>>(unitOfWork);
        RequestHandlerDelegate<Result<int>> next = _ => Task.FromResult(Result.Success(5));

        var result = await behavior.Handle(new GetValueQuery(), next, CancellationToken.None);

        await unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Commit_conflict_is_surfaced_as_a_conflict_failure()
    {
        var unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(Result.Conflict(new Error("concurrency.conflict", "xmin bentrok")));
        var behavior = new TransactionBehavior<DoubleValueCommand, Result<int>>(unitOfWork);
        RequestHandlerDelegate<Result<int>> next = _ => Task.FromResult(Result.Success(42));

        var result = await behavior.Handle(new DoubleValueCommand(21), next, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Conflict);
    }
}
