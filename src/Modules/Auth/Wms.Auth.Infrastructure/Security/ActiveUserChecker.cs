using Wms.Auth.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Auth.Infrastructure.Security;

// Adapter untuk mengecek apakah user masih aktif.
internal sealed class ActiveUserChecker(IUserReader userReader) : IActiveUserChecker
{
    public async Task<bool> IsActiveAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var id))
        {
            return false;
        }

        return await userReader.GetByIdAsync(id, cancellationToken) is not null;
    }
}
