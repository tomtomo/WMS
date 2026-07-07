using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.BuildingBlocks.Application.Abstractions;

// Gunakan system actor untuk proses tanpa user.
public sealed class SystemCurrentUser : ICurrentUser
{
    public string UserId => ICurrentUser.SystemActor;

    public bool IsAuthenticated => false;
}
