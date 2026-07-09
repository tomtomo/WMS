using Wms.Auth.Application.ReadModels;
using Wms.BuildingBlocks.Application.ReadModels;

namespace Wms.Auth.Application.Abstractions;

// Read port User
public interface IUserReader : IReader
{
    Task<UserDto?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    // User aktif yang punya role tertentu
    Task<IReadOnlyList<Guid>> GetUserIdsInRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<PagedResult<UserDto>> ListAsync(
        int page,
        int pageSize,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
}
