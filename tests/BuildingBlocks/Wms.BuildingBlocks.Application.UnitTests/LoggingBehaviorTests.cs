using AwesomeAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Wms.BuildingBlocks.Application.Behaviors;
using Wms.BuildingBlocks.Application.UnitTests.TestDoubles;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.BuildingBlocks.Application.UnitTests;

// Test LoggingBehavior
public sealed class LoggingBehaviorTests
{
    private static readonly ILogger<LoggingBehavior<DoubleValueCommand, Result<int>>> _logger =
        Substitute.For<ILogger<LoggingBehavior<DoubleValueCommand, Result<int>>>>();

    [Fact]
    public async Task Passes_through_the_success_result_unchanged()
    {
        var behavior = new LoggingBehavior<DoubleValueCommand, Result<int>>(_logger, TimeProvider.System);
        RequestHandlerDelegate<Result<int>> next = _ => Task.FromResult(Result.Success(42));

        var result = await behavior.Handle(new DoubleValueCommand(21), next, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task Passes_through_the_failure_result_unchanged()
    {
        var error = new Error("inventory.rejected", "ditolak");
        var behavior = new LoggingBehavior<DoubleValueCommand, Result<int>>(_logger, TimeProvider.System);
        RequestHandlerDelegate<Result<int>> next = _ => Task.FromResult(Result.Failure<int>(error));

        var result = await behavior.Handle(new DoubleValueCommand(21), next, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
    }
}
