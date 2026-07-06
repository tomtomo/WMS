using Microsoft.EntityFrameworkCore;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;

namespace Wms.Auth.Infrastructure.Persistence;

// Write side RefreshToken (tracked). Tanpa soft delete, tanpa query filter.
internal sealed class RefreshTokenRepository(AuthDbContext context) : IRefreshTokenRepository
{
    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        context.Set<RefreshToken>().Add(refreshToken);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetAsync(RefreshTokenId id, CancellationToken cancellationToken = default) =>
        context.Set<RefreshToken>().FirstOrDefaultAsync(token => token.Id == id, cancellationToken);

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        context.Set<RefreshToken>().FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
}
