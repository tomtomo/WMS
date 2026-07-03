using System.Diagnostics.CodeAnalysis;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.BuildingBlocks.Web.GrpcInterceptors;

// Carrier Result.Failure lintas boundary gRPC service, ditangkap ErrorMappingInterceptor lalu dimap ke RpcException.
[SuppressMessage(
    "Roslynator",
    "RCS1194:Implement exception constructors",
    Justification = "Carrier wajib membawa (ResultErrorType, Error); ctor exception standar justru merusak invariant + memunculkan jalur Error null.")]
public sealed class ResultFailureException(ResultErrorType errorType, Error error)
    : Exception(RequireMessage(error))
{
    public ResultErrorType ErrorType { get; } = errorType;

    public Error Error { get; } = error;

    private static string RequireMessage(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return error.Message;
    }
}
