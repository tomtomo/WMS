using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Choreography.IntegrationTests.TestSupport;

// Current user tetap untuk host modul
internal sealed class TestCurrentUser : ICurrentUser
{
    public const string TestUserId = "test-operator";

    public string UserId => TestUserId;

    public bool IsAuthenticated => true;
}
