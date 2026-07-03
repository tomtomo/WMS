using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.IntegrationTests.TestSupport;

public sealed class FakeRecurringJob : IRecurringJob
{
    public Task ExecuteAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
