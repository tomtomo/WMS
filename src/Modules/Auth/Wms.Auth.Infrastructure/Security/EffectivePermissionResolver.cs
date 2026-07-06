using Microsoft.EntityFrameworkCore;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;

namespace Wms.Auth.Infrastructure.Security;

// Menyusun permission yang dimiliki user berdasarkan role.
internal sealed class EffectivePermissionResolver(AuthDbContext context) : IEffectivePermissionResolver
{
    // Mengembalikan daftar permission code untuk user.
    public static async Task<IReadOnlyList<string>> ResolveCodesAsync(
        AuthDbContext context,
        IReadOnlyCollection<Guid> permissionIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (permissionIds.Count == 0)
        {
            return [];
        }

        var typedIds = permissionIds.Select(id => PermissionId.Create(id).Value).ToList();
        var permissions = await context.Set<Permission>().AsNoTracking()
            .Where(permission => typedIds.Contains(permission.Id))
            .ToListAsync(cancellationToken);

        return [.. permissions.Select(permission => permission.Code.Value).OrderBy(code => code, StringComparer.Ordinal)];
    }

    public async Task<IReadOnlyCollection<string>> ResolveAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.RoleIds.Count == 0)
        {
            return [];
        }

        var roleIds = user.RoleIds.Select(id => RoleId.Create(id).Value).ToList();
        var roles = await context.Set<Role>().AsNoTracking()
            .Where(role => roleIds.Contains(role.Id))
            .ToListAsync(cancellationToken);

        var permissionIds = roles.SelectMany(role => role.PermissionIds).Distinct().ToList();
        return await ResolveCodesAsync(context, permissionIds, cancellationToken);
    }
}
