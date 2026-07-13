using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Wms.Auth.IntegrationTests.TestSupport;

// Buat token Entra khusus test dengan RSA dan JWKS statis agar test tidak mengakses jaringan.
internal static class TestEntraTokens
{
    public const string Issuer = "https://login.microsoftonline.com/test-tenant/v2.0";

    public const string Audience = "test-client-id";

    private const string KeyId = "test-key-1";

    private static readonly RSA _signingKey = RSA.Create(2048);

    // Gunakan key publik test sebagai JWKS tanpa mengambil metadata dari Entra.
    public static IConfigurationManager<OpenIdConnectConfiguration> ConfigurationManager()
    {
        var configuration = new OpenIdConnectConfiguration { Issuer = Issuer };
        configuration.SigningKeys.Add(new RsaSecurityKey(_signingKey) { KeyId = KeyId });
        return new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
    }

    public static string Mint(
        string objectId,
        string? issuer = null,
        string? audience = null,
        DateTime? expires = null,
        string? preferredUsername = "operator@contoso.com",
        string? name = "Operator Satu",
        bool includeObjectId = true,
        RSA? signingKey = null)
    {
        var claims = new Dictionary<string, object>(StringComparer.Ordinal);
        if (includeObjectId)
        {
            claims["oid"] = objectId;
        }

        if (preferredUsername is not null)
        {
            claims["preferred_username"] = preferredUsername;
        }

        if (name is not null)
        {
            claims["name"] = name;
        }

        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = issuer ?? Issuer,
            Audience = audience ?? Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires ?? now.AddMinutes(10),
            Claims = claims,
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(signingKey ?? _signingKey) { KeyId = KeyId },
                SecurityAlgorithms.RsaSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    // Buat key lain untuk test penolakan token dengan signature yang tidak dikenal.
    public static RSA CreateForeignKey() => RSA.Create(2048);
}
