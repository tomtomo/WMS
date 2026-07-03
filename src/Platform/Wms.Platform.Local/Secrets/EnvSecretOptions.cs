using System.ComponentModel.DataAnnotations;

namespace Wms.Platform.Local.Secrets;

// Section "LocalPlatform:Secrets".
public sealed class EnvSecretOptions
{
    public const string SectionName = "LocalPlatform:Secrets";

    // Satu-satunya secret yang boleh fallback.
    [Required]
    public string SigningKeySecretName { get; set; } = "jwt-signing-key";
}
