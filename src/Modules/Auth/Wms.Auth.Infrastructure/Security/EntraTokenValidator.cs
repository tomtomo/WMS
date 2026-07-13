using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Wms.Auth.Application.Abstractions;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Infrastructure.Security;

// Validasi token Entra melalui metadata OIDC, termasuk signature, issuer, audience, dan masa berlaku. Jika metadata tidak tersedia, token tetap ditolak.
internal sealed class EntraTokenValidator(
    IOptions<EntraAuthOptions> options,
    IConfigurationManager<OpenIdConnectConfiguration> configurationManager) : IEntraTokenValidator
{
    private readonly EntraAuthOptions _options = options.Value;

    public async Task<Result<EntraIdentity>> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return Result.Invalid<EntraIdentity>(new Error("auth.entra_disabled", "Entra tidak aktif."));
        }

        if (string.IsNullOrWhiteSpace(idToken))
        {
            return Result.Invalid<EntraIdentity>(new Error("auth.entra_token_invalid", "id_token kosong."));
        }

        var configuration = await configurationManager.GetConfigurationAsync(cancellationToken);

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = configuration.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.ClientId,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = configuration.SigningKeys,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256],
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        var validation = await new JsonWebTokenHandler().ValidateTokenAsync(idToken, parameters);
        if (!validation.IsValid)
        {
            return Result.Invalid<EntraIdentity>(new Error("auth.entra_token_invalid", "id_token Entra tidak valid."));
        }

        // Gunakan claim oid sebagai identitas tetap untuk menautkan akun Entra ke user
        var objectId = ClaimValue(validation, "oid");
        if (string.IsNullOrWhiteSpace(objectId))
        {
            return Result.Invalid<EntraIdentity>(new Error("auth.entra_token_invalid", "Klaim oid tidak ada di id_token."));
        }

        return Result.Success(new EntraIdentity(
            objectId,
            ClaimValue(validation, "preferred_username"),
            ClaimValue(validation, "name")));
    }

    private static string? ClaimValue(TokenValidationResult result, string claimType) =>
        result.Claims.TryGetValue(claimType, out var value) ? value?.ToString() : null;
}
