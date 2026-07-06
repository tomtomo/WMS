using Wms.Auth.Domain;

namespace Wms.Auth.Application.Abstractions;

// Write side RefreshToken (tracked)
public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetAsync(RefreshTokenId id, CancellationToken cancellationToken = default);

    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);
}
