using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Auth.IntegrationTests.TestSupport;

// Implementasi ICurrentUser untuk test
internal sealed class FixedCurrentUser : ICurrentUser
{
    public const string TestUserId = "test-admin";

    public string UserId => TestUserId;

    public bool IsAuthenticated => true;
}
