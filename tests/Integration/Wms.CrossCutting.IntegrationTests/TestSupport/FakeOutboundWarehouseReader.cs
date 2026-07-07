using Wms.Outbound.Application.Abstractions;

namespace Wms.CrossCutting.IntegrationTests.TestSupport;

internal sealed class FakeOutboundWarehouseReader : IWarehouseReader
{
    public Task<bool> ExistsAsync(Guid warehouseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(true);
}
