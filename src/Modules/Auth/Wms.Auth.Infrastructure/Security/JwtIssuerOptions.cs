using System.ComponentModel.DataAnnotations;

namespace Wms.Auth.Infrastructure.Security;

// Konfigurasi JWT issuer.
public sealed class JwtIssuerOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = "wms";

    [Required]
    public string Audience { get; set; } = "wms";

    [Range(1, 1440)]
    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    [Required]
    public string SigningKeySecretName { get; set; } = "jwt-signing-key";
}
