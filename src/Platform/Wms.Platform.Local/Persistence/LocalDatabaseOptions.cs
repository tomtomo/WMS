using System.ComponentModel.DataAnnotations;

namespace Wms.Platform.Local.Persistence;

// Postgres Local dipakai idempotency/projection store + Hangfire storage. Section "LocalPlatform:Database".
public sealed class LocalDatabaseOptions
{
    public const string SectionName = "LocalPlatform:Database";

    [Required]
    public string ConnectionStringName { get; set; } = "wms";
}
