using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Cosmos;

namespace Wms.Platform.Azure.Persistence;

// Section "AzurePlatform:Persistence:Cosmos". Konfigurasi Cosmos untuk menyimpan telemetry operasional, bukan projection.
// Read model tetap menggunakan PostgreSQL.
public sealed class CosmosOptions
{
    public const string SectionName = "AzurePlatform:Persistence:Cosmos";

    // Endpoint dan Managed Identity di cloud, connection string hanya untuk emulator/test.
    public Uri? AccountEndpoint { get; set; }

    public string ConnectionStringName { get; set; } = "cosmos";

    [Required]
    public string DatabaseName { get; set; } = "wms";

    [Required]
    public string TelemetryContainerName { get; set; } = "operational-telemetry";

    // Gunakan Session consistency karena telemetry tidak harus langsung tersedia setelah disimpan.
    public ConsistencyLevel ConsistencyLevel { get; set; } = ConsistencyLevel.Session;
}
