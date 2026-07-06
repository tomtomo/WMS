using Wms.Notifications.Deliveries;

namespace Wms.Notifications.Subscriptions;

// Target notifikasi hasil resolve subscription.
public sealed record ResolvedTarget(Guid UserId, Channel Channel, Guid SubscriptionId);
