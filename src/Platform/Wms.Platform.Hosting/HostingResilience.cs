using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Infrastructure.Resilience;

namespace Microsoft.Extensions.Hosting;

// Default semua HttpClient: standard resilience. Klien gRPC s2s mengoverride profilnya via AddGrpcResilience per klien.
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
