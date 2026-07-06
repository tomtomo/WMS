using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Contracts;

namespace Wms.Notifications.Consumers;

// Mengirim notifikasi saat stok mendekati masa kedaluwarsa.
public sealed class StockNearExpiryConsumer(
    IInboxGuard inbox,
    NotificationFanout fanout,
    IUnitOfWork unitOfWork)
{
    public const string HandlerType = "notifications.stock_near_expiry";

    public async Task<Result> ConsumeAsync(
        StockNearExpiry integrationEvent,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (await inbox.HasProcessedAsync(eventId, HandlerType, cancellationToken))
        {
            return Result.Success();
        }

        var content = new NotificationContent(
            "Stok mendekati expiry",
            $"SKU {integrationEvent.Sku} batch {integrationEvent.Batch} exp {integrationEvent.Expiry:yyyy-MM-dd} — {integrationEvent.DaysToExpiry} hari lagi.",
            nameof(StockNearExpiry));
        await fanout.FanOutAsync(
            NotificationTopics.StockNearExpiry, content, integrationEvent.WarehouseId, eventId.ToString("N"), cancellationToken);

        await inbox.MarkProcessedAsync(eventId, HandlerType, cancellationToken);
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
