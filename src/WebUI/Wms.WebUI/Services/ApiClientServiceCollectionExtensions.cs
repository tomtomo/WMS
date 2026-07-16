using Microsoft.Extensions.DependencyInjection;
using Wms.WebUI.Services.Apis;

namespace Wms.WebUI.Services;

public static class ApiClientServiceCollectionExtensions
{
    // Gunakan scoped lifetime agar client mengikuti umur circuit; konfigurasi gateway sudah disiapkan AddWebUiBff.
    public static IServiceCollection AddWmsApiClients(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<MasterDataApi>();
        services.AddScoped<ReportingApi>();
        services.AddScoped<OutboundApi>();
        services.AddScoped<InboundApi>();
        services.AddScoped<InventoryApi>();
        services.AddScoped<NotificationsApi>();
        services.AddScoped<WmsApiClient>();
        services.AddScoped<WarehouseNameResolver>();
        services.AddScoped<LocationNameResolver>();
        return services;
    }
}
