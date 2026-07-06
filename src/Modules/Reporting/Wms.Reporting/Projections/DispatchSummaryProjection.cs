using Wms.Inventory.Contracts;
using Wms.Reporting.Abstractions;
using Wms.Reporting.ReadModels;

namespace Wms.Reporting.Projections;

// Projection dispatch dari StockRemoved per (warehouse, period). Satu StockRemoved = satu wave. grup per warehouse.
public sealed class DispatchSummaryProjection(IProjectionStore store)
{
    public async Task ApplyAsync(StockRemoved integrationEvent, DateOnly period, CancellationToken cancellationToken = default)
    {
        foreach (var group in integrationEvent.Lines.GroupBy(line => line.WarehouseId))
        {
            var warehouseId = group.Key;
            var volume = group.Sum(line => line.Qty);

            await store.IncrementAsync<DispatchSummary>(
                [warehouseId, period],
                () => new DispatchSummary { WarehouseId = warehouseId, Period = period },
                summary =>
                {
                    summary.DispatchedVolume += volume;
                    summary.WaveThroughput += 1;
                },
                cancellationToken);
        }
    }
}
