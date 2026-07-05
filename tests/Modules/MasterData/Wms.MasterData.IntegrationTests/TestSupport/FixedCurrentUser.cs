using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.MasterData.IntegrationTests.TestSupport;

internal sealed class FixedCurrentUser : ICurrentUser
{
    public const string TestUserId = "test-admin";

    public string UserId => TestUserId;

    public bool IsAuthenticated => true;
}
