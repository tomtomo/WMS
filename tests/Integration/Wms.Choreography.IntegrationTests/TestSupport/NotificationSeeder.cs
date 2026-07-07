using Microsoft.Extensions.DependencyInjection;
using Wms.Notifications.Deliveries;
using Wms.Notifications.Persistence;
using Wms.Notifications.Subscriptions;

namespace Wms.Choreography.IntegrationTests.TestSupport;

// Seed subs notifikasi langsung ke DB Notifications
internal static class NotificationSeeder
{
    public static async Task SeedSubscriptionAsync(
        ServiceProvider provider,
        SubscriberType subscriberType,
        Guid subscriberId,
        string topic,
        Channel[] channels,
        Guid? warehouseScope = null)
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var subscription = NotificationSubscription.Create(
            SubscriptionId.Create(Guid.NewGuid()).Value, subscriberType, subscriberId, topic, channels, warehouseScope).Value;
        context.Set<NotificationSubscription>().Add(subscription);
        await context.SaveChangesAsync();
    }
}
