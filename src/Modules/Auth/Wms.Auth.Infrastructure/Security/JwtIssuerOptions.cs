namespace Wms.Auth.Infrastructure.Security;

// Konfigurasi JWT issuer.
public sealed class JwtIssuerOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "wms";

    public string Audience { get; set; } = "wms";

    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    public string SigningKeySecretName { get; set; } = "jwt-signing-key";
}
