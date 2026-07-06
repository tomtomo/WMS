using Wms.Auth.Domain;

namespace Wms.Auth.Application.Abstractions;

// Membuat access token JWT RS256. validasi di BB.Web (AddJwtBearer).
public interface IJwtTokenIssuer
{
    Task<AccessToken> IssueAsync(
        User user,
        IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken = default);
}

// Access token dan kapan kedaluwarsa
public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);
