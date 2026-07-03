using Microsoft.AspNetCore.Http;

namespace Wms.BuildingBlocks.Web;

// Nama tunggal correlation-id lintas REST header / log scope / gRPC metadata.
public static class CorrelationId
{
    public const string HeaderName = "X-Correlation-ID";

    // Label properti di log scope
    public const string LogScopeKey = "CorrelationId";

    private const string ItemsKey = "Wms.CorrelationId";

    public static string? Get(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        return httpContext.Items.TryGetValue(ItemsKey, out var value) ? value as string : null;
    }

    public static void Set(HttpContext httpContext, string correlationId)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        httpContext.Items[ItemsKey] = correlationId;
    }
}
