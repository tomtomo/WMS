using Microsoft.AspNetCore.Http;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Web;

// Result gagal maka IResult RFC 7807 untuk endpoint minimal-API. CorrelationId diambil dari HttpContext.
public static class ResultHttpExtensions
{
    public static IResult ToProblem(this Result result, HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(httpContext);

        var problem = ProblemDetailsMapper.ToProblemDetails(
            result.ErrorType,
            result.Error,
            CorrelationId.Get(httpContext));

        return Results.Problem(problem);
    }
}
