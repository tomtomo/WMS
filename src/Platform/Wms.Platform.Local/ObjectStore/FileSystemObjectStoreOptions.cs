using System.ComponentModel.DataAnnotations;

namespace Wms.Platform.Local.ObjectStore;

// Section "LocalPlatform:ObjectStore".
public sealed class FileSystemObjectStoreOptions
{
    public const string SectionName = "LocalPlatform:ObjectStore";

    [Required]
    public string RootPath { get; set; } = string.Empty;

    // Alamat publik file endpoint Local
    [Required]
    public Uri? BaseUrl { get; set; }

    // Base64 32-byte. kosong = generate per-process (URL mati saat restart).
    public string? SigningKeyBase64 { get; set; }
}
