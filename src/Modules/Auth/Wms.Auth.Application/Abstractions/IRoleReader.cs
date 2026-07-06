using Wms.Auth.Application.ReadModels;
using Wms.BuildingBlocks.Application.ReadModels;

namespace Wms.Auth.Application.Abstractions;

// Read port Role
public interface IRoleReader : IReader
{
    Task<RoleDto?> GetByIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<PagedResult<RoleDto>> ListAsync(
        int page,
        int pageSize,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
}
