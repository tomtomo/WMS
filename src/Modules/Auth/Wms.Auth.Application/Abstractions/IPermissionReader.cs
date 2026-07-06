using Wms.Auth.Application.ReadModels;
using Wms.BuildingBlocks.Application.ReadModels;

namespace Wms.Auth.Application.Abstractions;

// Read port Permission
public interface IPermissionReader : IReader
{
    Task<IReadOnlyList<PermissionDto>> ListAsync(CancellationToken cancellationToken = default);
}
