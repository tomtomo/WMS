using Microsoft.EntityFrameworkCore;
using Wms.Notifications.Abstractions;
using Wms.Notifications.Subscriptions;

namespace Wms.Notifications.Persistence;

// Read port subscription aktif per eventType. AsNoTracking
internal sealed class NotificationSubscriptionReader(NotificationsDbContext context) : INotificationSubscriptionReader
{
    public async Task<IReadOnlyList<SubscriptionMatch>> ListForEventAsync(
        string eventType,
        CancellationToken cancellationToken = default)
    {
        var subscriptions = await context.Set<NotificationSubscription>().AsNoTracking()
            .Where(subscription => subscription.EventType == eventType)
            .ToListAsync(cancellationToken);

        return [.. subscriptions.Select(subscription => new SubscriptionMatch(
            subscription.Id.Value,
            subscription.SubscriberType,
            subscription.SubscriberId,
            subscription.Channels,
            subscription.WarehouseScope))];
    }
}
