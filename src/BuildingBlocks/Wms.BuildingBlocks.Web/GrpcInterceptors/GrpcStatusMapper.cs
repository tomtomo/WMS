using Grpc.Core;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Web.GrpcInterceptors;

// Titik map tunggal Result/Error
public static class GrpcStatusMapper
{
    public const string ErrorCodeTrailer = "error-code";

    public static StatusCode ToStatusCode(ResultErrorType errorType) => errorType switch
    {
        ResultErrorType.Validation => StatusCode.InvalidArgument,
        ResultErrorType.NotFound => StatusCode.NotFound,
        ResultErrorType.Conflict => StatusCode.Aborted,

        _ => StatusCode.FailedPrecondition,
    };

    public static RpcException ToRpcException(ResultErrorType errorType, Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var trailers = new Metadata { { ErrorCodeTrailer, error.Code } };
        return new RpcException(new Status(ToStatusCode(errorType), error.Message), trailers);
    }
}
