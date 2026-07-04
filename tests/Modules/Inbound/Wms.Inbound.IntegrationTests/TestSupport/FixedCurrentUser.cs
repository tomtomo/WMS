using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Inbound.IntegrationTests.TestSupport;

internal sealed class FixedCurrentUser : ICurrentUser
{
    public const string TestUserId = "test-operator";

    public string UserId => TestUserId;

    public bool IsAuthenticated => true;
}
