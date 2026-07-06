using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Notifications.IntegrationTests.TestSupport;

internal sealed class FixedCurrentUser : ICurrentUser
{
    public const string TestUserId = "test-admin";

    public string UserId => TestUserId;

    public bool IsAuthenticated => true;
}
