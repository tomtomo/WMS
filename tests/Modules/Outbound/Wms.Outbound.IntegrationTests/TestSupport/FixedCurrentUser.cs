using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Outbound.IntegrationTests.TestSupport;

internal sealed class FixedCurrentUser : ICurrentUser
{
    public const string TestUserId = "test-operator";

    public string UserId => TestUserId;

    public bool IsAuthenticated => true;

    // Test double = authorized actor, lewati enforcement authZ dan warehouse scope.
    public bool CanBypassWarehouseScope => true;

    public bool HasPermission(string permission) => true;
}
