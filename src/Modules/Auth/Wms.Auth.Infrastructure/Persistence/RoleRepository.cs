using Microsoft.EntityFrameworkCore;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;

namespace Wms.Auth.Infrastructure.Persistence;

// Write side Role (tracked).
internal sealed class RoleRepository(AuthDbContext context) : IRoleRepository
{
    public Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        context.Set<Role>().Add(role);
        return Task.CompletedTask;
    }

    public Task<Role?> GetAsync(RoleId id, CancellationToken cancellationToken = default) =>
        context.Set<Role>().IgnoreQueryFilters().FirstOrDefaultAsync(role => role.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        context.Set<Role>().IgnoreQueryFilters().AnyAsync(role => role.Code == code, cancellationToken);
}
