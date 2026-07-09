using Wms.BuildingBlocks.Infrastructure.Resilience;

namespace Microsoft.Extensions.DependencyInjection;

// Helper untuk HTTP internal antar service. Handler resilience bawaan dibersihkan dulu, lalu dipasang ulang profil HTTP supaya tidak dobel.
public static class InternalHttpClient
{
    public static IHttpClientBuilder AddInternalHttpClient(this IServiceCollection services, string name, Uri baseAddress)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(baseAddress);

#pragma warning disable EXTEXP0001
        return services
            .AddHttpClient(name, client => client.BaseAddress = baseAddress)
            .RemoveAllResilienceHandlers()
            .AddHttpResilience();
#pragma warning restore EXTEXP0001
    }
}
