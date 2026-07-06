using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.Notifications.Consumers;
using Wms.Notifications.IntegrationTests.TestSupport;
using Wms.Notifications.Persistence;
using Wms.Notifications.Persistence.Seed;
using Wms.Notifications.Subscriptions;
using Xunit;

namespace Wms.Notifications.IntegrationTests;

// Test seeding subscription notifikasi.
[Collection(PostgresCollection.Name)]
public sealed class NotificationSeederTests(PostgresFixture postgres) : NotificationsTestBase(postgres)
{
    [Fact]
    public async Task Seeder_creates_default_subscriptions_idempotently()
    {
        await SeedDefaultsAsync();
        var afterFirst = await CountSubscriptionsAsync();
        afterFirst.Should().BeGreaterThan(0);

        await SeedDefaultsAsync();
        var afterSecond = await CountSubscriptionsAsync();
        afterSecond.Should().Be(afterFirst, "seeder idempoten — run kedua tak menduplikasi");
    }

    [Fact]
    public async Task Seeder_maps_default_policy_roles_to_topics()
    {
        await SeedDefaultsAsync();

        var supervisorGr = await QueryAsync(context => context.Set<NotificationSubscription>().AnyAsync(subscription =>
            subscription.SubscriberId == NotificationRoles.Supervisor
            && subscription.EventType == NotificationTopics.GoodsReceiptPendingReview));
        supervisorGr.Should().BeTrue("SPV subscribe GR pending review (default policy)");

        var purchasingOverDelivery = await QueryAsync(context => context.Set<NotificationSubscription>().AnyAsync(subscription =>
            subscription.SubscriberId == NotificationRoles.Purchasing
            && subscription.EventType == NotificationTopics.GoodsReceiptOverDelivery));
        purchasingOverDelivery.Should().BeTrue("Purchasing subscribe over-delivery (default policy)");
    }

    private Task<int> CountSubscriptionsAsync() =>
        QueryAsync(context => context.Set<NotificationSubscription>().IgnoreQueryFilters().CountAsync());

    private Task SeedDefaultsAsync() =>
        ScopedAsync(async provider =>
        {
            await NotificationSubscriptionSeeder.SeedAsync(provider.GetRequiredService<NotificationsDbContext>());
            return true;
        });
}
