using Wms.Outbound.Application.Abstractions;

namespace Wms.Outbound.IntegrationTests.TestSupport;

// Fake Master Data
internal sealed class FakeWarehouseReader : IWarehouseReader
{
    private readonly HashSet<Guid> _unknownWarehouses = [];

    public void MarkUnknown(Guid warehouseId) => _unknownWarehouses.Add(warehouseId);

    public Task<bool> ExistsAsync(Guid warehouseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(!_unknownWarehouses.Contains(warehouseId));
}
