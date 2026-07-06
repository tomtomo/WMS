using Wms.Notifications.Deliveries;

namespace Wms.Notifications.Abstractions;

// Write side delivery, tracked.
public interface INotificationDeliveryRepository
{
    Task AddAsync(NotificationDelivery delivery, CancellationToken cancellationToken = default);

    // Klaim id batch Pending. AsNoTracking.
    Task<IReadOnlyList<DeliveryId>> ListPendingIdsAsync(int maxBatch, CancellationToken cancellationToken = default);

    // Tracked load by id agar MarkSent/ MarkFailed ikut SaveChanges.
    Task<NotificationDelivery?> GetAsync(DeliveryId id, CancellationToken cancellationToken = default);
}
