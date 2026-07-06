using Wms.Notifications.Deliveries;
using Wms.Notifications.Subscriptions;

namespace Wms.Notifications.UnitTests.TestData;

// Helper untuk membuat data test Notifications.
internal static class NotificationMother
{
    public static NotificationDelivery PendingDelivery(
        Channel channel = Channel.InApp,
        Guid? subscriptionId = null,
        Guid? userId = null)
    {
        var id = DeliveryId.Create(Guid.NewGuid()).Value;
        return NotificationDelivery.Enqueue(
            id,
            subscriptionId,
            userId ?? Guid.NewGuid(),
            channel,
            title: "GR pending review",
            body: "GR menunggu review SPV.",
            eventType: "GoodsReceiptPendingReview",
            warehouseId: Guid.NewGuid(),
            eventRef: Guid.NewGuid().ToString("N")).Value;
    }

    public static NotificationSubscription ActiveSubscription(
        SubscriberType subscriberType = SubscriberType.Role,
        Guid? subscriberId = null,
        string eventType = "GoodsReceiptPendingReview",
        IReadOnlyList<Channel>? channels = null,
        Guid? warehouseScope = null)
    {
        var id = SubscriptionId.Create(Guid.NewGuid()).Value;
        return NotificationSubscription.Create(
            id,
            subscriberType,
            subscriberId ?? Guid.NewGuid(),
            eventType,
            channels ?? [Channel.InApp, Channel.Push],
            warehouseScope).Value;
    }
}
