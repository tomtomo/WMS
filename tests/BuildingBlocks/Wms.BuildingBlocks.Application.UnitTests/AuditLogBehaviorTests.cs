using MediatR;
using NSubstitute;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Behaviors;
using Wms.BuildingBlocks.Application.UnitTests.TestDoubles;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.BuildingBlocks.Application.UnitTests;

// Test AuditLogBehavior
public sealed class AuditLogBehaviorTests
{
    [Fact]
    public async Task Mutating_command_success_records_entry_with_actor_and_action()
    {
        var store = Substitute.For<IAuditLogStore>();
        var currentUser = Substitute.For<ICurrentUser>();
        currentUser.UserId.Returns("operator-1");
        var behavior = new AuditLogBehavior<DoubleValueCommand, Result<int>>(store, currentUser, TimeProvider.System);
        RequestHandlerDelegate<Result<int>> next = _ => Task.FromResult(Result.Success(42));

        await behavior.Handle(new DoubleValueCommand(21), next, CancellationToken.None);

        await store.Received(1).RecordAsync(
            Arg.Is<AuditLogEntry>(entry => entry.Actor == "operator-1" && entry.Action == "DoubleValueCommand"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failed_command_records_no_audit_entry()
    {
        var store = Substitute.For<IAuditLogStore>();
        var behavior = new AuditLogBehavior<DoubleValueCommand, Result<int>>(
            store, Substitute.For<ICurrentUser>(), TimeProvider.System);
        RequestHandlerDelegate<Result<int>> next =
            _ => Task.FromResult(Result.Failure<int>(new Error("inventory.rejected", "ditolak")));

        await behavior.Handle(new DoubleValueCommand(21), next, CancellationToken.None);

        await store.DidNotReceive().RecordAsync(Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Query_records_no_audit_entry()
    {
        var store = Substitute.For<IAuditLogStore>();
        var behavior = new AuditLogBehavior<GetValueQuery, Result<int>>(
            store, Substitute.For<ICurrentUser>(), TimeProvider.System);
        RequestHandlerDelegate<Result<int>> next = _ => Task.FromResult(Result.Success(5));

        await behavior.Handle(new GetValueQuery(), next, CancellationToken.None);

        await store.DidNotReceive().RecordAsync(Arg.Any<AuditLogEntry>(), Arg.Any<CancellationToken>());
    }
}
