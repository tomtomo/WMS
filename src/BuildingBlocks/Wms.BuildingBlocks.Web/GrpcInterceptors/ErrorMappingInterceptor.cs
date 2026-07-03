using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Wms.BuildingBlocks.Web.GrpcInterceptors;

// Server-side: Result.Failure via ResultFailureException.
public sealed class ErrorMappingInterceptor(ILogger<ErrorMappingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);

        try
        {
            return await continuation(request, context);
        }
        catch (ResultFailureException failure)
        {
            throw GrpcStatusMapper.ToRpcException(failure.ErrorType, failure.Error);
        }
        catch (RpcException)
        {
            // Teruskan apa adanya, agar tidak wrap ganda.
            throw;
        }
#pragma warning disable S2221
        // Tangkap semua yang tak terduga supaya detail internal tak bocor.
        catch (Exception ex)
#pragma warning restore S2221
        {
            logger.LogError(ex, "Unhandled exception in gRPC handler {Method}.", context.Method);
            throw new RpcException(new Status(StatusCode.Internal, "Internal server error."));
        }
    }
}
