using Wms.Inbound.Application.Abstractions;

namespace Wms.Choreography.IntegrationTests.TestSupport;

internal sealed class FakeInboundWarehouseReader : IWarehouseReader
{
    public Task<bool> ExistsAsync(Guid warehouseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
