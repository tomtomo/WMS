using System.ComponentModel.DataAnnotations;

namespace Wms.BuildingBlocks.Web.Auth;

// Konfigurasi JWT RS256 offline: hanya public key (verify), issuer, audience.
public sealed class JwtBearerRs256Options
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    public string Audience { get; set; } = string.Empty;

    [Required]
    public string PublicKeyPem { get; set; } = string.Empty;
}
