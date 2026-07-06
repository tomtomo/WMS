using Wms.Inbound.Contracts;
using Wms.Inventory.Contracts;
using Wms.Reporting.Abstractions;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Projections;

// Projection on hand fisik
public sealed class StockOnHandProjection(IProjectionStore store)
{
    public async Task ApplyReceivedAsync(GRConfirmed integrationEvent, CancellationToken cancellationToken = default)
    {
        // Semua receivedLines
        foreach (var line in integrationEvent.ReceivedLines)
        {
            await AddQtyAsync(integrationEvent.WarehouseId, line.Sku, line.Batch, line.Qty, cancellationToken);
        }
    }

    public async Task ApplyRemovedAsync(StockRemoved integrationEvent, CancellationToken cancellationToken = default)
    {
        // Dispatch
        foreach (var line in integrationEvent.Lines)
        {
            await AddQtyAsync(line.WarehouseId, line.Sku, line.Batch, -line.Qty, cancellationToken);
        }
    }

    private Task AddQtyAsync(Guid warehouseId, string sku, string? batch, decimal delta, CancellationToken cancellationToken)
    {
        var batchKey = batch ?? string.Empty;
        return store.IncrementAsync<StockOnHandView>(
            [warehouseId, sku, batchKey],
            () => new StockOnHandView { WarehouseId = warehouseId, Sku = sku, Batch = batchKey },
            view => view.QtyOnHand += delta,
            cancellationToken);
    }
}
