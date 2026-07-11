using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Cosmos;

namespace Wms.Platform.Azure.Persistence;

// Section "AzurePlatform:Persistence:Cosmos".
// Konfigurasi Cosmos DB untuk projection store dan change feed.
public sealed class CosmosOptions
{
    public const string SectionName = "AzurePlatform:Persistence:Cosmos";

    // Gunakan endpoint dengan Managed Identity di production, connection string hanya untuk emulator dan test.
    public Uri? AccountEndpoint { get; set; }

    public string ConnectionStringName { get; set; } = "cosmos";

    [Required]
    public string DatabaseName { get; set; } = "wms";

    [Required]
    public string ProjectionContainerName { get; set; } = "projections";

    // Container ini menyimpan posisi terakhir change feed untuk setiap instance.
    [Required]
    public string LeaseContainerName { get; set; } = "projections-leases";

    [Required]
    public string ChangeFeedProcessorName { get; set; } = "projection-downstream";

    // Gunakan nama instance yang berbeda agar posisi baca tiap host tidak campur.
    [Required]
    public string ChangeFeedInstanceName { get; set; } = Environment.MachineName;

    public TimeSpan ChangeFeedPollInterval { get; set; } = TimeSpan.FromSeconds(1);

    // Konsistensi Session cukup untuk read side yang tidak harus langsung sinkron.
    public ConsistencyLevel ConsistencyLevel { get; set; } = ConsistencyLevel.Session;

    // Batas maksimum keterlambatan pemrosesan change feed sebelum dianggap bermasalah.
    public TimeSpan ChangeFeedLagThreshold { get; set; } = TimeSpan.FromSeconds(30);
}
