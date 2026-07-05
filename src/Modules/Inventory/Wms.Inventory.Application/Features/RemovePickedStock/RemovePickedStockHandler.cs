using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Contracts;
using Wms.Inventory.Contracts.Payloads;
using Wms.Outbound.Contracts;

namespace Wms.Inventory.Application.Features.RemovePickedStock;

// Efek ShipmentDispatched: hapus semua Stock Picked terikat wave lalu emit StockRemoved
public sealed class RemovePickedStockHandler(
    IStockRepository stockRepository,
    IIntegrationEventOutbox outbox)
{
    public async Task<Result> HandleAsync(ShipmentDispatched integrationEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var picked = await stockRepository.GetPickedByWaveAsync(integrationEvent.WaveId, cancellationToken);
        if (picked.Count == 0)
        {
            // Dispatch ulang wave yang sudah bersih
            return Result.Success();
        }

        var lines = picked
            .Select(stock => new StockRemovedLine(stock.WarehouseId, stock.Sku.Value, stock.Batch.Value, stock.Qty))
            .ToList();

        foreach (var stock in picked)
        {
            stockRepository.Remove(stock);
        }

        await outbox.AddAsync(new StockRemoved(integrationEvent.WaveId, lines), StockRemoved.DeliveryClass, cancellationToken);

        return Result.Success();
    }
}
