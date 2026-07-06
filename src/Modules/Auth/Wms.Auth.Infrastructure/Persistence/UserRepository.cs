using Microsoft.EntityFrameworkCore;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;

namespace Wms.Auth.Infrastructure.Persistence;

// Write side User (tracked).
internal sealed class UserRepository(AuthDbContext context) : IUserRepository
{
    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Set<User>().Add(user);
        return Task.CompletedTask;
    }

    public Task<User?> GetAsync(UserId id, CancellationToken cancellationToken = default) =>
        context.Set<User>().IgnoreQueryFilters().FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default) =>
        context.Set<User>().IgnoreQueryFilters().FirstOrDefaultAsync(user => user.Username == username, cancellationToken);

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default) =>
        context.Set<User>().IgnoreQueryFilters().AnyAsync(user => user.Username == username, cancellationToken);
}
