using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Web;

// Titik map tunggal Error/Result
public static class ProblemDetailsMapper
{
    public static int ToStatusCode(ResultErrorType errorType) => errorType switch
    {
        ResultErrorType.Validation => StatusCodes.Status400BadRequest,
        ResultErrorType.NotFound => StatusCodes.Status404NotFound,
        ResultErrorType.Conflict => StatusCodes.Status409Conflict,

        // request valid, aturan bisnis tidak
        _ => StatusCodes.Status422UnprocessableEntity,
    };

    public static ProblemDetails ToProblemDetails(ResultErrorType errorType, Error error, string? correlationId = null)
    {
        ArgumentNullException.ThrowIfNull(error);

        var status = ToStatusCode(errorType);
        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrases.GetReasonPhrase(status),
            Type = TypeFor(status),
            Detail = error.Message,
        };

        problem.Extensions["errorCode"] = error.Code;
        if (!string.IsNullOrEmpty(correlationId))
        {
            problem.Extensions["correlationId"] = correlationId;
        }

        return problem;
    }

    // URL rujukan
    private static string TypeFor(int status) => status switch
    {
        StatusCodes.Status404NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        StatusCodes.Status409Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        StatusCodes.Status422UnprocessableEntity => "https://tools.ietf.org/html/rfc9110#section-15.5.21",
        _ => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
    };
}
