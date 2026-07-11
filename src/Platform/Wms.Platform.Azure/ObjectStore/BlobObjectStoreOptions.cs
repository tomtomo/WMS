using System.ComponentModel.DataAnnotations;

namespace Wms.Platform.Azure.ObjectStore;

// Section "AzurePlatform:ObjectStore".
// Konfigurasi Blob Storage untuk penyimpanan file dan pembuatan URL akses sementara.
public sealed class BlobObjectStoreOptions
{
    public const string SectionName = "AzurePlatform:ObjectStore";

    [Required]
    public Uri? AccountUrl { get; set; }

    [Required]
    public string ContainerName { get; set; } = "gr-attachments";

    // Update user delegation key sebelum masa berlakunya habis agar URL baru tetap valid.
    public TimeSpan UserDelegationKeyLifetime { get; set; } = TimeSpan.FromHours(1);

    // Beri toleransi untuk perbedaan waktu antara host dan Azure Storage.
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);
}
