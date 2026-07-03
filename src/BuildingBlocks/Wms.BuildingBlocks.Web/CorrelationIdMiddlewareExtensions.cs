using Wms.BuildingBlocks.Web;

namespace Microsoft.AspNetCore.Builder;

// Pasang correlation id di awal pipeline, sebelum endpoint.
public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
