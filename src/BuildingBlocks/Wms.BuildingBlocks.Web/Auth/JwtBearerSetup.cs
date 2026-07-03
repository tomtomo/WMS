using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace Wms.BuildingBlocks.Web.Auth;

// Build TokenValidationParameters JWT RS256 alg-pinned dari public key offline.
public static class JwtBearerSetup
{
    public static TokenValidationParameters BuildValidationParameters(JwtBearerRs256Options options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.PublicKeyPem))
        {
            throw new InvalidOperationException(
                $"{JwtBearerRs256Options.SectionName}:{nameof(JwtBearerRs256Options.PublicKeyPem)} wajib diisi (fail-secure).");
        }

        var rsa = RSA.Create();
        rsa.ImportFromPem(options.PublicKeyPem);

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(rsa),

            // Alg-pin RS256: tolak HS256/none/alg lain
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            RequireSignedTokens = true,

            ValidateIssuer = true,
            ValidIssuer = options.Issuer,
            ValidateAudience = true,
            ValidAudience = options.Audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    }
}
