using AwesomeAssertions;
using Wms.Notifications.Consumers;
using Wms.Notifications.Deliveries;
using Wms.Notifications.IntegrationTests.TestSupport;
using Wms.Notifications.Subscriptions;
using Xunit;

namespace Wms.Notifications.IntegrationTests;

// Memastikan notifikasi dikirim ke target subscription yang sesuai.
[Collection(PostgresCollection.Name)]
public sealed class FanOutBranchingTests(PostgresFixture postgres) : NotificationsTestBase(postgres)
{
    [Fact]
    public async Task Gr_pending_review_without_over_delivery_notifies_supervisor_only()
    {
        var supervisor = Guid.NewGuid();
        var purchaser = Guid.NewGuid();
        await GivenGoodsReceiptSubscribers(supervisor, purchaser);

        var result = await DeliverAsync<GoodsReceiptPendingReviewConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.GrPendingReview(Guid.NewGuid(), hasOverDelivery: false), Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        var deliveries = await AllDeliveriesAsync();
        deliveries.Should().OnlyContain(delivery => delivery.UserId == supervisor);
        deliveries.Select(delivery => delivery.Channel).Should().BeEquivalentTo([Channel.InApp, Channel.Push]);
    }

    [Fact]
    public async Task Gr_pending_review_with_over_delivery_adds_purchasing_email()
    {
        var supervisor = Guid.NewGuid();
        var purchaser = Guid.NewGuid();
        await GivenGoodsReceiptSubscribers(supervisor, purchaser);

        await DeliverAsync<GoodsReceiptPendingReviewConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.GrPendingReview(Guid.NewGuid(), hasOverDelivery: true), Guid.NewGuid()));

        var deliveries = await AllDeliveriesAsync();
        deliveries.Should().Contain(delivery => delivery.UserId == supervisor && delivery.Channel == Channel.InApp);
        deliveries.Should().Contain(delivery => delivery.UserId == supervisor && delivery.Channel == Channel.Push);
        deliveries.Should().ContainSingle(delivery => delivery.UserId == purchaser && delivery.Channel == Channel.Email);
    }

    [Fact]
    public async Task Wave_ready_notifies_only_matching_warehouse_supervisor()
    {
        var warehouse = Guid.NewGuid();
        var supervisor = Guid.NewGuid();
        var role = Guid.NewGuid();
        Directory.SetRoleMembers(role, supervisor);
        await SeedSubscriptionAsync(SubscriberType.Role, role, NotificationTopics.WaveReady, [Channel.InApp, Channel.Push], warehouse);

        await DeliverAsync<WaveReadyConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.WaveReady(Guid.NewGuid()), Guid.NewGuid())); // warehouse lain
        (await AllDeliveriesAsync()).Should().BeEmpty("warehouseScope tak cocok");

        await DeliverAsync<WaveReadyConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.WaveReady(warehouse), Guid.NewGuid())); // warehouse cocok
        (await AllDeliveriesAsync()).Should().OnlyContain(delivery => delivery.UserId == supervisor);
    }

    [Fact]
    public async Task Stock_near_expiry_notifies_planner_and_supervisor_by_email()
    {
        var planner = Guid.NewGuid();
        var supervisor = Guid.NewGuid();
        var plannerRole = Guid.NewGuid();
        var supervisorRole = Guid.NewGuid();
        Directory.SetRoleMembers(plannerRole, planner);
        Directory.SetRoleMembers(supervisorRole, supervisor);
        await SeedSubscriptionAsync(SubscriberType.Role, plannerRole, NotificationTopics.StockNearExpiry, [Channel.Email]);
        await SeedSubscriptionAsync(SubscriberType.Role, supervisorRole, NotificationTopics.StockNearExpiry, [Channel.Email]);

        await DeliverAsync<StockNearExpiryConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.NearExpiry(Guid.NewGuid()), Guid.NewGuid()));

        var deliveries = await AllDeliveriesAsync();
        deliveries.Should().HaveCount(2);
        deliveries.Should().OnlyContain(delivery => delivery.Channel == Channel.Email);
        deliveries.Select(delivery => delivery.UserId).Should().BeEquivalentTo([planner, supervisor]);
    }

    [Fact]
    public async Task Stock_allocation_with_shortfall_notifies_supervisor_and_purchasing()
    {
        var supervisor = Guid.NewGuid();
        var purchaser = Guid.NewGuid();
        await GivenShortfallSubscribers(supervisor, purchaser);

        await DeliverAsync<StockAllocationCompletedConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.AllocationCompleted(withShortfall: true), Guid.NewGuid()));

        var deliveries = await AllDeliveriesAsync();
        deliveries.Should().Contain(delivery => delivery.UserId == supervisor && delivery.Channel == Channel.InApp);
        deliveries.Should().Contain(delivery => delivery.UserId == purchaser && delivery.Channel == Channel.Email);
    }

    [Fact]
    public async Task Stock_allocation_without_shortfall_is_noop()
    {
        var supervisor = Guid.NewGuid();
        var purchaser = Guid.NewGuid();
        await GivenShortfallSubscribers(supervisor, purchaser);

        var result = await DeliverAsync<StockAllocationCompletedConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.AllocationCompleted(withShortfall: false), Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        (await AllDeliveriesAsync()).Should().BeEmpty("tanpa shortfall = no-op, nol delivery");
    }

    private async Task GivenGoodsReceiptSubscribers(Guid supervisor, Guid purchaser)
    {
        var supervisorRole = Guid.NewGuid();
        var purchasingRole = Guid.NewGuid();
        Directory.SetRoleMembers(supervisorRole, supervisor);
        Directory.SetRoleMembers(purchasingRole, purchaser);
        await SeedSubscriptionAsync(
            SubscriberType.Role, supervisorRole, NotificationTopics.GoodsReceiptPendingReview, [Channel.InApp, Channel.Push]);
        await SeedSubscriptionAsync(
            SubscriberType.Role, purchasingRole, NotificationTopics.GoodsReceiptOverDelivery, [Channel.Email]);
    }

    private async Task GivenShortfallSubscribers(Guid supervisor, Guid purchaser)
    {
        var supervisorRole = Guid.NewGuid();
        var purchasingRole = Guid.NewGuid();
        Directory.SetRoleMembers(supervisorRole, supervisor);
        Directory.SetRoleMembers(purchasingRole, purchaser);
        await SeedSubscriptionAsync(SubscriberType.Role, supervisorRole, NotificationTopics.StockShortfall, [Channel.InApp]);
        await SeedSubscriptionAsync(SubscriberType.Role, purchasingRole, NotificationTopics.StockShortfall, [Channel.Email]);
    }
}
