using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Eventing.IntegrationTests.TestSupport;

// Current user bawaan untuk proses background.
internal sealed class SystemCurrentUser : ICurrentUser
{
    public string UserId => ICurrentUser.SystemActor;

    public bool IsAuthenticated => false;
}
