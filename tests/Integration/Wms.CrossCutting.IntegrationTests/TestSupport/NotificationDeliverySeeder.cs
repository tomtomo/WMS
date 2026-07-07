using Microsoft.Extensions.DependencyInjection;
using Wms.Notifications.Deliveries;
using Wms.Notifications.Persistence;
using Wms.Notifications.Subscriptions;

namespace Wms.CrossCutting.IntegrationTests.TestSupport;

// Seed satu delivery in app Pending langsung ke DB Notifications.
internal static class NotificationDeliverySeeder
{
    public static async Task<Guid> SeedInAppDeliveryAsync(ServiceProvider provider, Guid userId)
    {
        var delivery = NotificationDelivery.Enqueue(
            DeliveryId.Create(Guid.NewGuid()).Value,
            subscriptionId: null,
            userId,
            Channel.InApp,
            "Wave siap",
            "Wave siap dieksekusi.",
            "WaveReady",
            warehouseId: null,
            eventRef: "evt-1").Value;

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        context.Set<NotificationDelivery>().Add(delivery);
        await context.SaveChangesAsync();
        return delivery.Id.Value;
    }
}
