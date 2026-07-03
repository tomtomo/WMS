using AwesomeAssertions;
using Microsoft.AspNetCore.Http;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Web;
using Xunit;

namespace Wms.BuildingBlocks.Web.Tests;

public sealed class ProblemDetailsMapperTests
{
    [Theory]
    [InlineData(ResultErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ResultErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ResultErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ResultErrorType.Failure, StatusCodes.Status422UnprocessableEntity)]
    public void Error_class_maps_to_the_right_http_status(ResultErrorType errorType, int expectedStatus)
    {
        ProblemDetailsMapper.ToStatusCode(errorType).Should().Be(expectedStatus);
    }

    [Fact]
    public void ProblemDetails_carries_rfc7807_shape_with_error_code_and_correlation()
    {
        var error = new Error("inventory.not_found", "Stok tidak ditemukan.");

        var problem = ProblemDetailsMapper.ToProblemDetails(ResultErrorType.NotFound, error, "corr-123");

        problem.Status.Should().Be(StatusCodes.Status404NotFound);
        problem.Title.Should().Be("Not Found");
        problem.Type.Should().Contain("section-15.5.5");
        problem.Detail.Should().Be("Stok tidak ditemukan.");
        problem.Extensions["errorCode"].Should().Be("inventory.not_found");
        problem.Extensions["correlationId"].Should().Be("corr-123");
    }

    [Fact]
    public void Error_code_is_echoed_verbatim_and_correlation_omitted_when_absent()
    {
        // xmin concurrency
        var error = new Error("concurrency.conflict", "xmin bentrok.");

        var problem = ProblemDetailsMapper.ToProblemDetails(ResultErrorType.Conflict, error);

        problem.Status.Should().Be(StatusCodes.Status409Conflict);
        problem.Extensions["errorCode"].Should().Be("concurrency.conflict");
        problem.Extensions.Should().NotContainKey("correlationId");
    }
}
