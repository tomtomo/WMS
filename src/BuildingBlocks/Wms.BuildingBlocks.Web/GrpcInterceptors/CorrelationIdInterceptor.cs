using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Wms.BuildingBlocks.Web.GrpcInterceptors;

// Server-side: read x-correlation-id metadata, generate jika tidak ada, push ke OTel log scope
public sealed class CorrelationIdInterceptor(ILogger<CorrelationIdInterceptor> logger) : Interceptor
{
    public const string MetadataKey = "x-correlation-id";

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(continuation);

        var correlationId = ResolveCorrelationId(context);
        context.ResponseTrailers.Add(MetadataKey, correlationId);

        var scope = new Dictionary<string, object> { [CorrelationId.LogScopeKey] = correlationId };
        using (logger.BeginScope(scope))
        {
            return await continuation(request, context);
        }
    }

    private static string ResolveCorrelationId(ServerCallContext context)
    {
        var inbound = context.RequestHeaders.GetValue(MetadataKey);
        return string.IsNullOrWhiteSpace(inbound) ? Guid.NewGuid().ToString() : inbound;
    }
}
