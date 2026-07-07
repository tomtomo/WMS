using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Infrastructure.Resilience;

namespace Microsoft.Extensions.Hosting;

// Default untuk HttpClient biasa. gRPC internal pakai AddInternalGrpcClient agar tidak kena dua resilience handler.
public static class HostingResilience
{
    public static IHostApplicationBuilder AddDefaultHttpClientDefaults(this IHostApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddHttpResilience();
            http.AddServiceDiscovery();
        });

        return builder;
    }
}
