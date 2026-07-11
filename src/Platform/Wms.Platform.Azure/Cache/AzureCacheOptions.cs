using System.ComponentModel.DataAnnotations;

namespace Wms.Platform.Azure.Cache;

// Konfigurasi cache Azure hanya menyimpan endpoint Redis, sedangkan adapternya tetap memakai Platform.Shared.
public sealed class AzureCacheOptions
{
    public const string SectionName = "AzurePlatform:Cache";

    [Required]
    public string ConnectionStringName { get; set; } = "redis";
}
