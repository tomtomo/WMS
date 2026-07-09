using Microsoft.EntityFrameworkCore;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Application.ReadModels;
using Wms.Auth.Domain;
using Wms.BuildingBlocks.Application.ReadModels;

namespace Wms.Auth.Infrastructure.Persistence;

// Read port User — AsNoTracking
internal sealed class UserReader(AuthDbContext context, IEffectivePermissionResolver permissionResolver) : IUserReader
{
    public async Task<UserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var id = UserId.Create(userId);
        if (id.IsFailure)
        {
            return null;
        }

        var user = await context.Set<User>().AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id.Value, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var permissionCodes = await permissionResolver.ResolveAsync(user, cancellationToken);
        return Map(user, permissionCodes);
    }

    public async Task<IReadOnlyList<Guid>> GetUserIdsInRoleAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        // RoleIds disimpan sebagai jsonb lewat converter, jadi Contains belum bisa diterjemahkan EF ke SQL. (cloud: query jsonb GIN atau tabel membership)
        var users = await context.Set<User>().AsNoTracking().ToListAsync(cancellationToken);
        return [.. users.Where(user => user.RoleIds.Contains(roleId)).Select(user => user.Id.Value)];
    }

    public async Task<PagedResult<UserDto>> ListAsync(
        int page,
        int pageSize,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var currentPage = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 200);

        IQueryable<User> query = context.Set<User>().AsNoTracking();
        if (includeInactive)
        {
            query = query.IgnoreQueryFilters();
        }

        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(user => user.Username)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        var items = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            var permissionCodes = await permissionResolver.ResolveAsync(user, cancellationToken);
            items.Add(Map(user, permissionCodes));
        }

        return new PagedResult<UserDto>(items, total, currentPage, size);
    }

    private static UserDto Map(User user, IReadOnlyCollection<string> permissionCodes) =>
        new(
            user.Id.Value,
            user.Username,
            user.Email,
            user.IsActive,
            [.. user.RoleIds],
            [.. user.AssignedWarehouseIds],
            [.. permissionCodes]);
}
