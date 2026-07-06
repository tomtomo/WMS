using Wms.Notifications.Deliveries;

namespace Wms.Notifications.Subscriptions;

// Snapshot subscription untuk kebutuhan resolve notifikasi.
public sealed record SubscriptionMatch(
    Guid SubscriptionId,
    SubscriberType SubscriberType,
    Guid SubscriberId,
    IReadOnlyList<Channel> Channels,
    Guid? WarehouseScope);
