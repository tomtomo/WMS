using AwesomeAssertions;
using Wms.Notifications.Consumers;
using Wms.Notifications.Deliveries;
using Wms.Notifications.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Notifications.IntegrationTests;

// Test delivery langsung ke user tertentu.
[Collection(PostgresCollection.Name)]
public sealed class DirectDeliveryTests(PostgresFixture postgres) : NotificationsTestBase(postgres)
{
    [Fact]
    public async Task Putaway_task_assigned_delivers_direct_with_null_subscription()
    {
        var operatorId = Guid.NewGuid();

        await DeliverAsync<PutawayTaskAssignedConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.PutawayAssigned(operatorId, Guid.NewGuid()), Guid.NewGuid()));

        var deliveries = await AllDeliveriesAsync();
        deliveries.Should().OnlyContain(delivery => delivery.UserId == operatorId && delivery.SubscriptionId == null);
        deliveries.Select(delivery => delivery.Channel).Should().BeEquivalentTo([Channel.InApp, Channel.Push]);
    }

    [Fact]
    public async Task Picking_task_assigned_delivers_direct_with_null_subscription()
    {
        var operatorId = Guid.NewGuid();

        await DeliverAsync<PickingTaskAssignedConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.PickingAssigned(operatorId, Guid.NewGuid()), Guid.NewGuid()));

        var deliveries = await AllDeliveriesAsync();
        deliveries.Should().OnlyContain(delivery => delivery.UserId == operatorId && delivery.SubscriptionId == null);
        deliveries.Select(delivery => delivery.Channel).Should().BeEquivalentTo([Channel.InApp, Channel.Push]);
    }
}
