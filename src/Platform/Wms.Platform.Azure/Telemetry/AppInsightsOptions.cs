namespace Wms.Platform.Azure.Telemetry;

// Section "AzurePlatform:Telemetry".
// Konfigurasi Application Insights untuk telemetry Azure.
// Jika connection string tidak tersedia, telemetry tetap berjalan tanpa exporter Azure.
public sealed class AppInsightsOptions
{
    public const string SectionName = "AzurePlatform:Telemetry";

    public string ConnectionStringName { get; set; } = "appinsights";

    public string ServiceName { get; set; } = "wms";
}
