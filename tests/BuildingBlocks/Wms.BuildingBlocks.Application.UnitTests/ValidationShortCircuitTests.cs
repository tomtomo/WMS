using AwesomeAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using Wms.BuildingBlocks.Application.Behaviors;
using Wms.BuildingBlocks.Application.UnitTests.TestDoubles;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.BuildingBlocks.Application.UnitTests;

// Test ValidationBehavior
public sealed class ValidationShortCircuitTests
{
    private static readonly DoubleValueCommand _command = new(-1);

    [Fact]
    public async Task Validation_failure_short_circuits_without_invoking_the_handler()
    {
        var handlerInvoked = false;
        RequestHandlerDelegate<Result<int>> next = _ =>
        {
            handlerInvoked = true;
            return Task.FromResult(Result.Success(0));
        };

        var validator = Substitute.For<IValidator<DoubleValueCommand>>();
        validator
            .ValidateAsync(Arg.Any<DoubleValueCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("Value", "harus positif")]));
        var behavior = new ValidationBehavior<DoubleValueCommand, Result<int>>([validator]);

        var result = await behavior.Handle(_command, next, CancellationToken.None);

        handlerInvoked.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public async Task Validation_success_lets_the_handler_run()
    {
        var handlerInvoked = false;
        RequestHandlerDelegate<Result<int>> next = _ =>
        {
            handlerInvoked = true;
            return Task.FromResult(Result.Success(42));
        };

        var validator = Substitute.For<IValidator<DoubleValueCommand>>();
        validator
            .ValidateAsync(Arg.Any<DoubleValueCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        var behavior = new ValidationBehavior<DoubleValueCommand, Result<int>>([validator]);

        var result = await behavior.Handle(_command, next, CancellationToken.None);

        handlerInvoked.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task Void_command_validation_failure_returns_a_non_generic_invalid_result()
    {
        var handlerInvoked = false;
        RequestHandlerDelegate<Result> next = _ =>
        {
            handlerInvoked = true;
            return Task.FromResult(Result.Success());
        };

        var validator = Substitute.For<IValidator<VoidCommand>>();
        validator
            .ValidateAsync(Arg.Any<VoidCommand>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult([new ValidationFailure("X", "wajib")]));
        var behavior = new ValidationBehavior<VoidCommand, Result>([validator]);

        var result = await behavior.Handle(new VoidCommand(), next, CancellationToken.None);

        handlerInvoked.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
    }

    [Fact]
    public async Task No_validators_registered_lets_the_handler_run()
    {
        var handlerInvoked = false;
        RequestHandlerDelegate<Result<int>> next = _ =>
        {
            handlerInvoked = true;
            return Task.FromResult(Result.Success(7));
        };

        var behavior = new ValidationBehavior<DoubleValueCommand, Result<int>>([]);

        var result = await behavior.Handle(_command, next, CancellationToken.None);

        handlerInvoked.Should().BeTrue();
        result.IsSuccess.Should().BeTrue();
    }
}
