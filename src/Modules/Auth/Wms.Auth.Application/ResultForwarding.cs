using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Application;

internal static class ResultForwarding
{
    public static Result<TValue> ForwardFailure<TValue>(this Result source)
    {
        if (source.IsSuccess)
        {
            throw new InvalidOperationException("ForwardFailure hanya untuk Result gagal.");
        }

        return source.ErrorType switch
        {
            ResultErrorType.Validation => Result.Invalid<TValue>(source.Error),
            ResultErrorType.Conflict => Result.Conflict<TValue>(source.Error),
            ResultErrorType.NotFound => Result.NotFound<TValue>(source.Error),
            _ => Result.Failure<TValue>(source.Error),
        };
    }
}
