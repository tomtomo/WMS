using System.ComponentModel.DataAnnotations;

namespace Wms.Platform.Azure.Persistence;

// Section "AzurePlatform:Persistence:FlexibleServer".
// Konfigurasi koneksi PostgreSQL Flexible Server untuk setiap modul.
public sealed class FlexibleServerOptions
{
    public const string SectionName = "AzurePlatform:Persistence:FlexibleServer";

    // Setiap modul tetap memakai connection string database masing-masing.
    [Required]
    public string ConnectionStringName { get; set; } = "wms";

    // Password database dibaca dari Key Vault agar tidak disimpan di connection string.
    public string? PasswordSecretName { get; set; }

    // Wajibkan TLS dan verifikasi sertifikat server, nonaktifkan hanya untuk test lokal tanpa TLS.
    public bool RequireSsl { get; set; } = true;
}
