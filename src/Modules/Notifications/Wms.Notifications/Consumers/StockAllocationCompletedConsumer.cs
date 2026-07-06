using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Contracts;

namespace Wms.Notifications.Consumers;

// Mengirim notifikasi jika alokasi wave menghasilkan shortfall.
public sealed class StockAllocationCompletedConsumer(
    IInboxGuard inbox,
    NotificationFanout fanout,
    IUnitOfWork unitOfWork)
{
    public const string HandlerType = "notifications.stock_allocation_completed";

    public async Task<Result> ConsumeAsync(
        StockAllocationCompleted integrationEvent,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (await inbox.HasProcessedAsync(eventId, HandlerType, cancellationToken))
        {
            return Result.Success();
        }

        // Hanya kirim notifikasi jika ada shortfall.
        if (integrationEvent.Shortfalls.Count > 0)
        {
            var content = new NotificationContent(
                "Stok short saat alokasi",
                $"Wave {integrationEvent.WaveId:N} — {integrationEvent.Shortfalls.Count} line stok short.",
                nameof(StockAllocationCompleted));

            // Shortfall berlaku untuk semua warehouse karena event tidak membawa warehouseId.
            await fanout.FanOutAsync(
                NotificationTopics.StockShortfall, content, warehouseId: null, eventId.ToString("N"), cancellationToken);
        }

        await inbox.MarkProcessedAsync(eventId, HandlerType, cancellationToken);
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
