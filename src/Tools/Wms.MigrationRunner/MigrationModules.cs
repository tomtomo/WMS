using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Wms.MigrationRunner;

// Setiap modul punya connection string sendiri, jadi tabel infrastructure.* berada di database modul masing-masing.
internal static class MigrationModules
{
    // Nama connection string mengikuti nama resource DB di Aspire, bukan nama resource host seperti "wms-{module}".
    public const string Inbound = "inbounddb";
    public const string Inventory = "inventorydb";
    public const string Outbound = "outbounddb";
    public const string MasterData = "masterdatadb";
    public const string Auth = "authdb";
    public const string Reporting = "reportingdb";
    public const string Notifications = "notificationsdb";

    public static IServiceCollection AddAllModules(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInboundModule(configuration, Inbound);
        services.AddInventoryModule(configuration, Inventory);
        services.AddOutboundModule(configuration, Outbound);
        services.AddMasterDataModule(configuration, MasterData);
        services.AddAuthModule(configuration, Auth);
        services.AddReportingModule(configuration, Reporting);
        services.AddNotificationsModule(configuration, Notifications);
        return services;
    }
}
