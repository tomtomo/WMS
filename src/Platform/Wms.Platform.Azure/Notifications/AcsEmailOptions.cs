using System.ComponentModel.DataAnnotations;

namespace Wms.Platform.Azure.Notifications;

// Section "AzurePlatform:Notifications:Acs".
// Konfigurasi email ACS menggunakan endpoint dengan Managed Identity di production.
// Connection string tetap tersedia sebagai fallback, dan alamat pengirim harus berasal dari domain yang sudah diverifikasi.
public sealed class AcsEmailOptions
{
    public const string SectionName = "AzurePlatform:Notifications:Acs";

    public Uri? Endpoint { get; set; }

    [Required]
    public string ConnectionStringName { get; set; } = "acs";

    [Required]
    [EmailAddress]
    public string SenderAddress { get; set; } = string.Empty;
}
