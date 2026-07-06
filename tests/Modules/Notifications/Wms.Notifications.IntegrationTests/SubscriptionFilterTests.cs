using AwesomeAssertions;
using Wms.Notifications.Consumers;
using Wms.Notifications.Deliveries;
using Wms.Notifications.IntegrationTests.TestSupport;
using Wms.Notifications.Subscriptions;
using Xunit;

namespace Wms.Notifications.IntegrationTests;

// Test filter subscription aktif.
[Collection(PostgresCollection.Name)]
public sealed class SubscriptionFilterTests(PostgresFixture postgres) : NotificationsTestBase(postgres)
{
    [Fact]
    public async Task Deactivated_subscription_is_excluded_from_resolution()
    {
        var activeUser = Guid.NewGuid();
        var inactiveUser = Guid.NewGuid();
        await SeedSubscriptionAsync(SubscriberType.User, activeUser, NotificationTopics.WaveReady, [Channel.InApp]);
        await SeedSubscriptionAsync(
            SubscriberType.User, inactiveUser, NotificationTopics.WaveReady, [Channel.InApp], isActive: false);

        await DeliverAsync<WaveReadyConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.WaveReady(Guid.NewGuid()), Guid.NewGuid()));

        (await AllDeliveriesAsync()).Should().OnlyContain(delivery => delivery.UserId == activeUser);
    }
}
