using Microsoft.EntityFrameworkCore;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;

namespace Wms.Auth.Infrastructure.Persistence;

// Simpan dan cari akun eksternal yang terhubung ke user
internal sealed class UserExternalLoginRepository(AuthDbContext context) : IUserExternalLoginRepository
{
    public async Task<UserId?> FindUserIdAsync(
        string provider,
        string subject,
        CancellationToken cancellationToken = default)
    {
        var login = await context.Set<UserExternalLogin>()
            .AsNoTracking()
            .FirstOrDefaultAsync(entry => entry.Provider == provider && entry.Subject == subject, cancellationToken);
        return login?.UserId;
    }

    public Task<bool> ExistsAsync(string provider, string subject, CancellationToken cancellationToken = default) =>
        context.Set<UserExternalLogin>()
            .AnyAsync(entry => entry.Provider == provider && entry.Subject == subject, cancellationToken);

    public Task AddAsync(UserExternalLogin login, CancellationToken cancellationToken = default)
    {
        context.Set<UserExternalLogin>().Add(login);
        return Task.CompletedTask;
    }
}
