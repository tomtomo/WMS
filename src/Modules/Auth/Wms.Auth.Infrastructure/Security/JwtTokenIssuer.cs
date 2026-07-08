using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Auth.Infrastructure.Security;

// Membuat access token JWT menggunakan private key.
internal sealed class JwtTokenIssuer(
    ISecretProvider secretProvider,
    IOptions<JwtIssuerOptions> options,
    TimeProvider timeProvider) : IJwtTokenIssuer
{
    private readonly JwtIssuerOptions _options = options.Value;

    public async Task<AccessToken> IssueAsync(
        User user,
        IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(permissionCodes);

        var privateKeyPem = await secretProvider.GetSecretAsync(_options.SigningKeySecretName, cancellationToken);
        if (string.IsNullOrWhiteSpace(privateKeyPem))
        {
            // Jangan terbitkan token jika signing key tidak tersedia.
            throw new InvalidOperationException(
                $"Signing key '{_options.SigningKeySecretName}' tidak tersedia — fail-secure, token tidak diterbitkan.");
        }

        using var rsa = RSA.Create();
        rsa.ImportFromPem(privateKeyPem);

        // RSA dibuat per request, jadi signature provider tidak boleh di cache.
        var securityKey = new RsaSecurityKey(rsa)
        {
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false },
        };
        var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);

        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new(WmsClaimTypes.Subject, user.Id.Value.ToString()),
            new(WmsClaimTypes.Username, user.Username),
        };
        claims.AddRange(permissionCodes.Select(code => new Claim(WmsClaimTypes.Permission, code)));
        claims.AddRange(user.AssignedWarehouseIds.Select(id => new Claim(WmsClaimTypes.Warehouse, id.ToString())));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = signingCredentials,
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new AccessToken(token, expiresAt);
    }
}
