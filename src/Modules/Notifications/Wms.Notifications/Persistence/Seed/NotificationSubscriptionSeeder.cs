using Microsoft.EntityFrameworkCore;
using Wms.Notifications.Consumers;
using Wms.Notifications.Deliveries;
using Wms.Notifications.Subscriptions;

namespace Wms.Notifications.Persistence.Seed;

// Seed subscription default
public static class NotificationSubscriptionSeeder
{
    public static async Task SeedAsync(NotificationsDbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (await context.Set<NotificationSubscription>().IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        foreach (var (subscriberId, topic, channels) in Defaults())
        {
            var subscription = NotificationSubscription.Create(
                SubscriptionId.Create(Guid.NewGuid()).Value,
                SubscriberType.Role,
                subscriberId,
                topic,
                channels,
                warehouseScope: null);
            if (subscription.IsSuccess)
            {
                context.Set<NotificationSubscription>().Add(subscription.Value);
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    // Default policy.
    private static IEnumerable<(Guid SubscriberId, string Topic, IReadOnlyList<Channel> Channels)> Defaults() =>
    [
        (NotificationRoles.Supervisor, NotificationTopics.GoodsReceiptPendingReview, new[] { Channel.InApp }),
        (NotificationRoles.Purchasing, NotificationTopics.GoodsReceiptOverDelivery, new[] { Channel.Email }),
        (NotificationRoles.Supervisor, NotificationTopics.WaveReady, new[] { Channel.InApp }),
        (NotificationRoles.Supervisor, NotificationTopics.StockShortfall, new[] { Channel.InApp }),
        (NotificationRoles.Purchasing, NotificationTopics.StockShortfall, new[] { Channel.Email }),
        (NotificationRoles.InventoryPlanner, NotificationTopics.StockNearExpiry, new[] { Channel.Email }),
        (NotificationRoles.Supervisor, NotificationTopics.StockNearExpiry, new[] { Channel.Email }),
    ];
}
