using Microsoft.EntityFrameworkCore;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Application.ReadModels;
using Wms.Auth.Domain;
using Wms.Auth.Infrastructure.Security;
using Wms.BuildingBlocks.Application.ReadModels;

namespace Wms.Auth.Infrastructure.Persistence;

// Read port Role — AsNoTracking.
internal sealed class RoleReader(AuthDbContext context) : IRoleReader
{
    public async Task<RoleDto?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var id = RoleId.Create(roleId);
        if (id.IsFailure)
        {
            return null;
        }

        var role = await context.Set<Role>().AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id.Value, cancellationToken);
        if (role is null)
        {
            return null;
        }

        var codes = await EffectivePermissionResolver.ResolveCodesAsync(context, role.PermissionIds, cancellationToken);
        return Map(role, codes);
    }

    public async Task<PagedResult<RoleDto>> ListAsync(
        int page,
        int pageSize,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var currentPage = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 200);

        IQueryable<Role> query = context.Set<Role>().AsNoTracking();
        if (includeInactive)
        {
            query = query.IgnoreQueryFilters();
        }

        var total = await query.CountAsync(cancellationToken);
        var roles = await query
            .OrderBy(role => role.Code)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        var items = new List<RoleDto>(roles.Count);
        foreach (var role in roles)
        {
            var codes = await EffectivePermissionResolver.ResolveCodesAsync(context, role.PermissionIds, cancellationToken);
            items.Add(Map(role, codes));
        }

        return new PagedResult<RoleDto>(items, total, currentPage, size);
    }

    private static RoleDto Map(Role role, IReadOnlyList<string> permissionCodes) =>
        new(role.Id.Value, role.Code, role.Name, role.IsActive, permissionCodes);
}
