using Microsoft.EntityFrameworkCore;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Application.ReadModels;
using Wms.Auth.Domain;

namespace Wms.Auth.Infrastructure.Persistence;

// Read port Permission
internal sealed class PermissionReader(AuthDbContext context) : IPermissionReader
{
    public async Task<IReadOnlyList<PermissionDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var permissions = await context.Set<Permission>().AsNoTracking().ToListAsync(cancellationToken);

        return [.. permissions
            .Select(permission => new PermissionDto(permission.Id.Value, permission.Code.Value, permission.Description))
            .OrderBy(dto => dto.Code, StringComparer.Ordinal)];
    }
}
