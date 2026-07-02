using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Xunit;

namespace Wms.BuildingBlocks.Domain.UnitTests;

// Test Result: sukses/gagal, akses Value aman.
public sealed class ResultTests
{
    private static readonly Error _sampleError = new("inventory.insufficient_stock", "Stok tak cukup.");

    [Fact]
    public void Success_is_successful_and_carries_no_error()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Error.Should().Be(Error.None);
        result.ErrorType.Should().Be(ResultErrorType.None);
    }

    [Fact]
    public void Failure_is_not_successful_and_carries_the_error()
    {
        var result = Result.Failure(_sampleError);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(_sampleError);
        result.ErrorType.Should().Be(ResultErrorType.Failure);
    }

    [Fact]
    public void Generic_success_carries_the_value()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Accessing_value_on_failure_throws_invariant()
    {
        var result = Result.Failure<int>(_sampleError);

        var act = () => _ = result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Map_transforms_value_on_success()
    {
        var result = Result.Success(21).Map(x => x * 2);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Map_short_circuits_on_failure_without_running_the_projection()
    {
        var projectionRan = false;

        var result = Result.Failure<int>(_sampleError).Map(x =>
        {
            projectionRan = true;
            return x * 2;
        });

        projectionRan.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(_sampleError);
    }

    [Fact]
    public void Bind_chains_into_the_next_result_on_success()
    {
        var result = Result.Success(21).Bind(x => Result.Success(x * 2));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public void Bind_short_circuits_on_failure_without_running_the_binder()
    {
        var binderRan = false;

        var result = Result.Failure<int>(_sampleError).Bind(x =>
        {
            binderRan = true;
            return Result.Success(x * 2);
        });

        binderRan.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(_sampleError);
    }

    [Fact]
    public void Map_preserves_the_error_type_when_propagating_failure()
    {
        var result = Result.NotFound<int>(_sampleError).Map(x => x * 2);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.NotFound);
    }

    [Fact]
    public void Invalid_carries_the_validation_error_type()
    {
        var result = Result.Invalid(_sampleError);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Should().Be(_sampleError);
    }

    [Fact]
    public void Conflict_carries_the_conflict_error_type()
    {
        Result.Conflict(_sampleError).ErrorType.Should().Be(ResultErrorType.Conflict);
    }

    [Fact]
    public void NotFound_carries_the_not_found_error_type()
    {
        Result.NotFound(_sampleError).ErrorType.Should().Be(ResultErrorType.NotFound);
    }
}
