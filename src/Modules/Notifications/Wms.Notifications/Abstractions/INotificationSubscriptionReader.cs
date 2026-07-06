using Wms.Notifications.Subscriptions;

namespace Wms.Notifications.Abstractions;

// Read port subscription aktif per eventType
public interface INotificationSubscriptionReader
{
    Task<IReadOnlyList<SubscriptionMatch>> ListForEventAsync(string eventType, CancellationToken cancellationToken = default);
}
