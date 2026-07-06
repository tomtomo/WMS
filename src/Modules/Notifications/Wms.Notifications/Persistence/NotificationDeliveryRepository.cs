using Microsoft.EntityFrameworkCore;
using Wms.Notifications.Abstractions;
using Wms.Notifications.Deliveries;

namespace Wms.Notifications.Persistence;

// Write side delivery, tracked. Commit oleh consumer (IUnitOfWork)/ command pipeline/ dispatcher.
internal sealed class NotificationDeliveryRepository(NotificationsDbContext context) : INotificationDeliveryRepository
{
    public Task AddAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default)
    {
        context.Set<NotificationDelivery>().Add(delivery);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<DeliveryId>> ListPendingIdsAsync(
        int maxBatch,
        CancellationToken cancellationToken = default)
    {
        var pending = await context.Set<NotificationDelivery>().AsNoTracking()
            .Where(delivery => delivery.State == DeliveryState.Pending)
            .OrderBy(delivery => delivery.CreatedAt)
            .Take(maxBatch)
            .ToListAsync(cancellationToken);
        return [.. pending.Select(delivery => delivery.Id)];
    }

    public Task<NotificationDelivery?> GetAsync(DeliveryId id, CancellationToken cancellationToken = default) =>
        context.Set<NotificationDelivery>().FirstOrDefaultAsync(delivery => delivery.Id == id, cancellationToken);
}
