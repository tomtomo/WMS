using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.CrossCutting.IntegrationTests.TestSupport;

internal sealed class FixedCurrentUser : ICurrentUser
{
    public const string UserIdValue = "test-operator";

    public string UserId => UserIdValue;

    public bool IsAuthenticated => true;
}
