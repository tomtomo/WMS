using System.Diagnostics;
using AwesomeAssertions;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.Contracts.Abstractions;
using Wms.Eventing.IntegrationTests.TestSupport;
using Wms.Inbound.Contracts;
using Wms.Inbound.Contracts.Enums;
using Wms.Inbound.Contracts.Payloads;
using Wms.Inbound.Infrastructure;
using Wms.Inventory.Contracts;
using Wms.Inventory.Contracts.Enums;
using Wms.Inventory.Contracts.Payloads;
using Xunit;

namespace Wms.Eventing.IntegrationTests;

// Test routing event berdasarkan delivery class.
[Collection(EventingRailCollection.Name)]
public sealed class RailRoutingTests(EventingRailFixture fixture)
{
    private const string TraceparentPattern = "^00-[0-9a-f]{32}-[0-9a-f]{16}-[0-9a-f]{2}$";

    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan _leakWindow = TimeSpan.FromMilliseconds(500);

    [Fact]
    public async Task Dual_class_event_reaches_both_rails_as_two_envelopes_with_trace_intact()
    {
        var exchange = UniqueName("wms.events");
        await using var producer = EventingRailHost.BuildInboundProducer(
            await fixture.CreateFreshDatabaseAsync("producer"), fixture.RabbitMqConnectionString, exchange);
        await EventingRailHost.MigrateAsync<InboundDbContext>(producer);

        using var coreSubscriber = new RecordingSubscriber(fixture.RabbitMqConnectionString, exchange);
        using var notificationSubscriber = new RecordingSubscriber(fixture.RabbitMqConnectionString, exchange);
        await coreSubscriber.StartAsync(
            UniqueName("q.core"), [new RailSubscription(StockAllocationCompleted.LogicalName, DeliveryClass.CoreFlow)]);
        await notificationSubscriber.StartAsync(
            UniqueName("q.notif"), [new RailSubscription(StockAllocationCompleted.LogicalName, DeliveryClass.Notification)]);

        // Emit event yang sama ke dua delivery class dalam activity yang sama.
        var allocation = SampleAllocationCompleted();
        using (new Activity("test.emit").SetIdFormat(ActivityIdFormat.W3C).Start())
        {
            await EventingRailHost.EmitToOutboxAsync(producer, allocation, DeliveryClass.CoreFlow);
            await EventingRailHost.EmitToOutboxAsync(producer, allocation, DeliveryClass.Notification);
        }

        await EventingRailHost.DrainAsync(producer);
        await EventingRailHost.WaitUntilAsync(
            () => Task.FromResult(!coreSubscriber.Received.IsEmpty && !notificationSubscriber.Received.IsEmpty), _timeout);
        await Task.Delay(_leakWindow);

        coreSubscriber.Received.Should().ContainSingle();
        notificationSubscriber.Received.Should().ContainSingle();
        var core = coreSubscriber.Received.Single();
        var notification = notificationSubscriber.Received.Single();

        core.LogicalName.Should().Be(StockAllocationCompleted.LogicalName);
        notification.LogicalName.Should().Be(StockAllocationCompleted.LogicalName);
        core.DeliveryClass.Should().Be(DeliveryClass.CoreFlow);
        notification.DeliveryClass.Should().Be(DeliveryClass.Notification);
        core.EventId.Should().NotBe(notification.EventId, "dua envelope terpisah dari dua baris outbox");
        core.Traceparent.Should().MatchRegex(TraceparentPattern, "traceparent utuh round-trip RabbitMQ JSON");
        notification.Traceparent.Should().MatchRegex(TraceparentPattern);
    }

    [Fact]
    public async Task CoreFlow_and_notification_events_do_not_cross_leak()
    {
        var exchange = UniqueName("wms.events");
        await using var producer = EventingRailHost.BuildInboundProducer(
            await fixture.CreateFreshDatabaseAsync("producer"), fixture.RabbitMqConnectionString, exchange);
        await EventingRailHost.MigrateAsync<InboundDbContext>(producer);

        using var coreSubscriber = new RecordingSubscriber(fixture.RabbitMqConnectionString, exchange);
        using var notificationSubscriber = new RecordingSubscriber(fixture.RabbitMqConnectionString, exchange);
        await coreSubscriber.StartAsync(
            UniqueName("q.core"), [new RailSubscription(GRConfirmed.LogicalName, DeliveryClass.CoreFlow)]);
        await notificationSubscriber.StartAsync(
            UniqueName("q.notif"), [new RailSubscription(GoodsReceiptPendingReview.LogicalName, DeliveryClass.Notification)]);

        await EventingRailHost.EmitToOutboxAsync(producer, SampleGrConfirmed(), DeliveryClass.CoreFlow);
        await EventingRailHost.EmitToOutboxAsync(
            producer, new GoodsReceiptPendingReview(Guid.NewGuid(), Guid.NewGuid(), true, 1), DeliveryClass.Notification);
        await EventingRailHost.DrainAsync(producer);

        await EventingRailHost.WaitUntilAsync(
            () => Task.FromResult(!coreSubscriber.Received.IsEmpty && !notificationSubscriber.Received.IsEmpty), _timeout);
        await Task.Delay(_leakWindow);

        coreSubscriber.Received.Should().ContainSingle()
            .Which.LogicalName.Should().Be(GRConfirmed.LogicalName, "core queue hanya event CoreFlow-nya");
        notificationSubscriber.Received.Should().ContainSingle()
            .Which.LogicalName.Should().Be(GoodsReceiptPendingReview.LogicalName, "notif queue hanya event Notification-nya");
    }

    [Fact]
    public async Task Publisher_confirm_failure_leaves_outbox_row_pending()
    {
        var exchange = UniqueName("wms.events");

        // Gunakan broker yang tidak tersedia untuk memaksa publish gagal.
        await using var producer = EventingRailHost.BuildInboundProducer(
            await fixture.CreateFreshDatabaseAsync("producer"), "amqp://guest:guest@localhost:1", exchange);
        await EventingRailHost.MigrateAsync<InboundDbContext>(producer);

        await EventingRailHost.EmitToOutboxAsync(producer, SampleGrConfirmed(), DeliveryClass.CoreFlow);
        await EventingRailHost.DrainAsync(producer);

        var rows = await EventingRailHost.OutboxRowsAsync(producer);
        rows.Should().ContainSingle();
        rows[0].ProcessedAt.Should().BeNull("gagal-confirm/unreachable → row tetap Pending untuk retry poll berikut");
        rows[0].AttemptCount.Should().BeGreaterThan(0, "attempt di-increment saat publish gagal");
    }

    private static StockAllocationCompleted SampleAllocationCompleted() => new(
        Guid.NewGuid(),
        AllocationStatus.PartiallyAllocated,
        [new Allocation(Guid.NewGuid(), "SKU-A", Guid.NewGuid(), "B1", 8m, Guid.NewGuid(), Guid.NewGuid())],
        [new Shortfall(Guid.NewGuid(), "SKU-A", 10m, 8m, 2m)]);

    private static GRConfirmed SampleGrConfirmed() => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        [new ReceivedLine("SKU-A", 10m, "B1", null, ReceivedLineStatus.Good)],
        []);

    private static string UniqueName(string prefix) => $"{prefix}.{Guid.NewGuid():N}";
}
