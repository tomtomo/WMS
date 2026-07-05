using Wms.BuildingBlocks.Application.ReadModels;
using Wms.MasterData.Application.ReadModels;

namespace Wms.MasterData.Application.Abstractions;

// Read port Location
public interface ILocationReader : IReader
{
    Task<LocationDto?> GetByIdAsync(Guid locationId, CancellationToken cancellationToken = default);

    Task<PagedResult<LocationDto>> ListAsync(
        int page,
        int pageSize,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);
}
