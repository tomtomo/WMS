using System.ComponentModel.DataAnnotations;

namespace Wms.Platform.Azure.Secrets;

// Section "AzurePlatform:Secrets".
public sealed class KeyVaultOptions
{
    public const string SectionName = "AzurePlatform:Secrets";

    [Required]
    public Uri? VaultUri { get; set; }
}
