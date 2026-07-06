using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Notifications.Deliveries;
using Wms.Notifications.Subscriptions;
using Xunit;

namespace Wms.Notifications.UnitTests;

// Test NotificationSubscription
public sealed class NotificationSubscriptionTests
{
    private static readonly SubscriptionId _id = SubscriptionId.Create(Guid.NewGuid()).Value;

    [Fact]
    public void Create_sets_fields_active_with_no_events()
    {
        var subscriberId = Guid.NewGuid();
        var warehouse = Guid.NewGuid();

        var result = NotificationSubscription.Create(
            _id, SubscriberType.Role, subscriberId, "WaveReady", [Channel.InApp, Channel.Push], warehouse);

        result.IsSuccess.Should().BeTrue();
        var subscription = result.Value;
        subscription.SubscriberType.Should().Be(SubscriberType.Role);
        subscription.SubscriberId.Should().Be(subscriberId);
        subscription.EventType.Should().Be("WaveReady");
        subscription.Channels.Should().BeEquivalentTo([Channel.InApp, Channel.Push]);
        subscription.WarehouseScope.Should().Be(warehouse);
        subscription.IsActive.Should().BeTrue();
        subscription.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Create_allows_null_warehouse_scope()
    {
        var result = NotificationSubscription.Create(
            _id, SubscriberType.User, Guid.NewGuid(), "StockNearExpiry", [Channel.Email], warehouseScope: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.WarehouseScope.Should().BeNull();
    }

    [Fact]
    public void Create_rejects_blank_event_type()
    {
        var result = NotificationSubscription.Create(
            _id, SubscriberType.Role, Guid.NewGuid(), "  ", [Channel.InApp], null);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be("subscription.event_type_required");
    }

    [Fact]
    public void Create_rejects_empty_channels()
    {
        var result = NotificationSubscription.Create(
            _id, SubscriberType.Role, Guid.NewGuid(), "WaveReady", [], null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.channels_required");
    }

    [Fact]
    public void Create_rejects_empty_subscriber()
    {
        var result = NotificationSubscription.Create(
            _id, SubscriberType.Role, Guid.Empty, "WaveReady", [Channel.InApp], null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.subscriber_required");
    }

    [Fact]
    public void Update_changes_channels_and_scope()
    {
        var subscription = NotificationSubscription.Create(
            _id, SubscriberType.Role, Guid.NewGuid(), "WaveReady", [Channel.InApp], null).Value;
        var newWarehouse = Guid.NewGuid();

        var result = subscription.Update([Channel.Email, Channel.Push], newWarehouse);

        result.IsSuccess.Should().BeTrue();
        subscription.Channels.Should().BeEquivalentTo([Channel.Email, Channel.Push]);
        subscription.WarehouseScope.Should().Be(newWarehouse);
    }

    [Fact]
    public void Update_rejects_empty_channels()
    {
        var subscription = NotificationSubscription.Create(
            _id, SubscriberType.Role, Guid.NewGuid(), "WaveReady", [Channel.InApp], null).Value;

        var result = subscription.Update([], null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("subscription.channels_required");
    }

    [Fact]
    public void Deactivate_is_idempotent_soft_delete()
    {
        var subscription = NotificationSubscription.Create(
            _id, SubscriberType.Role, Guid.NewGuid(), "WaveReady", [Channel.InApp], null).Value;

        subscription.Deactivate().IsSuccess.Should().BeTrue();
        subscription.IsActive.Should().BeFalse();
        subscription.Deactivate().IsSuccess.Should().BeTrue();
        subscription.IsActive.Should().BeFalse();
    }
}
