using System.Security.Cryptography;
using System.Text;
using Wms.Auth.Application.Abstractions;

namespace Wms.Auth.Infrastructure.Security;

// Membuat refresh token beserta hash nya.
internal sealed class RefreshTokenFactory : IRefreshTokenFactory
{
    private const int TokenSizeBytes = 32;

    public RefreshTokenMaterial Create()
    {
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenSizeBytes));
        return new RefreshTokenMaterial(rawToken, Hash(rawToken));
    }

    public string Hash(string rawToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash);
    }
}
