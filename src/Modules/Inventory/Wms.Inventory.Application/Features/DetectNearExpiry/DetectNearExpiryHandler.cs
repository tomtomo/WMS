using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Contracts;

namespace Wms.Inventory.Application.Features.DetectNearExpiry;

// Scan: balance Available/OnHand dengan expiry ≤ now, threshold, lalu emit
// StockNearExpiry per balance. Pakai TimeProvider. Stok tidak berubah state.
internal sealed class DetectNearExpiryHandler(
    IStockReader stockReader,
    IIntegrationEventOutbox outbox,
    TimeProvider timeProvider) : ICommandHandler<DetectNearExpiryCommand>
{
    public async Task<Result> Handle(DetectNearExpiryCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var threshold = today.AddDays(command.ThresholdDays);

        var expiring = await stockReader.GetExpiringAsync(threshold, cancellationToken);
        foreach (var stock in expiring)
        {
            var daysToExpiry = stock.Expiry.DayNumber - today.DayNumber;
            await outbox.AddAsync(
                new StockNearExpiry(
                    stock.StockId,
                    stock.Sku,
                    stock.WarehouseId,
                    stock.LocationId,
                    stock.Batch,
                    stock.Expiry,
                    stock.Qty,
                    daysToExpiry),
                StockNearExpiry.DeliveryClass,
                cancellationToken);
        }

        return Result.Success();
    }
}
