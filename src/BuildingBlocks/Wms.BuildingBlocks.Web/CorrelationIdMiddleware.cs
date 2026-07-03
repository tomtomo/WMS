using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Wms.BuildingBlocks.Web;

// Correlation-id per request: baca X-Correlation-ID, generate bila tidak ada, push ke OTel log scope
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = ResolveCorrelationId(context);
        CorrelationId.Set(context, correlationId);

        // Set header tepat sebelum response flush, aman dari "headers read-only".
        context.Response.OnStarting(
            static state =>
            {
                var ctx = (HttpContext)state;
                var id = CorrelationId.Get(ctx);
                if (!string.IsNullOrEmpty(id))
                {
                    ctx.Response.Headers[CorrelationId.HeaderName] = id;
                }

                return Task.CompletedTask;
            },
            context);

        var scope = new Dictionary<string, object> { [CorrelationId.LogScopeKey] = correlationId };
        using (logger.BeginScope(scope))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        var inbound = context.Request.Headers[CorrelationId.HeaderName].ToString();
        return string.IsNullOrWhiteSpace(inbound) ? Guid.NewGuid().ToString() : inbound;
    }
}
