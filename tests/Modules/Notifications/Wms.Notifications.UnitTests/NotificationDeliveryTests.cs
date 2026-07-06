using AwesomeAssertions;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Notifications.Deliveries;
using Wms.Notifications.UnitTests.TestData;
using Xunit;

namespace Wms.Notifications.UnitTests;

// Test state transition NotificationDelivery.
public sealed class NotificationDeliveryTests
{
    private static readonly DeliveryId _id = DeliveryId.Create(Guid.NewGuid()).Value;

    [Fact]
    public void Enqueue_starts_pending_with_fields_set()
    {
        var userId = Guid.NewGuid();
        var warehouseId = Guid.NewGuid();
        var subscriptionId = Guid.NewGuid();

        var result = NotificationDelivery.Enqueue(
            _id, subscriptionId, userId, Channel.Email, "title", "body", "WaveReady", warehouseId, "evt-1");

        result.IsSuccess.Should().BeTrue();
        var delivery = result.Value;
        delivery.State.Should().Be(DeliveryState.Pending);
        delivery.SubscriptionId.Should().Be(subscriptionId);
        delivery.UserId.Should().Be(userId);
        delivery.Channel.Should().Be(Channel.Email);
        delivery.Title.Should().Be("title");
        delivery.Body.Should().Be("body");
        delivery.EventType.Should().Be("WaveReady");
        delivery.WarehouseId.Should().Be(warehouseId);
        delivery.EventRef.Should().Be("evt-1");
        delivery.ProviderMessageId.Should().BeNull();
        delivery.RetryCount.Should().Be(0);
        delivery.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Enqueue_direct_delivery_has_null_subscription()
    {
        var result = NotificationDelivery.Enqueue(
            _id, subscriptionId: null, Guid.NewGuid(), Channel.Push, "t", "b", "PutawayTaskAssigned", null, "evt");

        result.IsSuccess.Should().BeTrue();
        result.Value.SubscriptionId.Should().BeNull();
    }

    [Theory]
    [InlineData("", "body", "evt", "delivery.title_required")]
    [InlineData("title", "", "evt", "delivery.body_required")]
    [InlineData("title", "body", "", "delivery.event_ref_required")]
    public void Enqueue_rejects_blank_required_fields(string title, string body, string eventRef, string expectedCode)
    {
        var result = NotificationDelivery.Enqueue(
            _id, null, Guid.NewGuid(), Channel.InApp, title, body, "WaveReady", null, eventRef);

        result.IsFailure.Should().BeTrue();
        result.ErrorType.Should().Be(ResultErrorType.Validation);
        result.Error.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Enqueue_rejects_empty_user()
    {
        var result = NotificationDelivery.Enqueue(
            _id, null, Guid.Empty, Channel.InApp, "t", "b", "WaveReady", null, "evt");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("delivery.user_required");
    }

    [Fact]
    public void Enqueue_rejects_blank_event_type()
    {
        var result = NotificationDelivery.Enqueue(
            _id, null, Guid.NewGuid(), Channel.InApp, "t", "b", "  ", null, "evt");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("delivery.event_type_required");
    }

    [Fact]
    public void MarkSent_moves_pending_to_sent_and_records_provider_id()
    {
        var delivery = NotificationMother.PendingDelivery(Channel.Email);

        var result = delivery.MarkSent("provider-123");

        result.IsSuccess.Should().BeTrue();
        delivery.State.Should().Be(DeliveryState.Sent);
        delivery.ProviderMessageId.Should().Be("provider-123");
    }

    [Fact]
    public void MarkSent_accepts_null_provider_id()
    {
        var delivery = NotificationMother.PendingDelivery(Channel.InApp);

        var result = delivery.MarkSent(providerMessageId: null);

        result.IsSuccess.Should().BeTrue();
        delivery.State.Should().Be(DeliveryState.Sent);
        delivery.ProviderMessageId.Should().BeNull();
    }

    [Fact]
    public void MarkSent_is_idempotent_after_sent()
    {
        var delivery = NotificationMother.PendingDelivery(Channel.Email);
        delivery.MarkSent("first");

        var again = delivery.MarkSent("second");

        again.IsSuccess.Should().BeTrue();
        delivery.State.Should().Be(DeliveryState.Sent);
        delivery.ProviderMessageId.Should().Be("first", "re-send tak boleh menimpa provider id pertama");
    }

    [Fact]
    public void MarkFailed_moves_to_failed_and_records_reason_and_retry()
    {
        var delivery = NotificationMother.PendingDelivery(Channel.Push);

        var result = delivery.MarkFailed("provider timeout", retryCount: 3);

        result.IsSuccess.Should().BeTrue();
        delivery.State.Should().Be(DeliveryState.Failed);
        delivery.FailureReason.Should().Be("provider timeout");
        delivery.RetryCount.Should().Be(3);
    }

    [Fact]
    public void MarkFailed_then_MarkSent_recovers_to_sent()
    {
        var delivery = NotificationMother.PendingDelivery(Channel.Push);
        delivery.MarkFailed("transient", 1);

        var result = delivery.MarkSent("recovered");

        result.IsSuccess.Should().BeTrue();
        delivery.State.Should().Be(DeliveryState.Sent);
    }

    [Fact]
    public void MarkFailed_rejects_blank_reason()
    {
        var delivery = NotificationMother.PendingDelivery();

        var result = delivery.MarkFailed("  ", 1);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("delivery.failure_reason_required");
    }

    [Fact]
    public void MarkRead_marks_inapp_delivery_read()
    {
        var delivery = NotificationMother.PendingDelivery(Channel.InApp);
        delivery.MarkSent(null);

        var result = delivery.MarkRead();

        result.IsSuccess.Should().BeTrue();
        delivery.State.Should().Be(DeliveryState.Read);
    }

    [Theory]
    [InlineData(Channel.Email)]
    [InlineData(Channel.Push)]
    public void MarkRead_rejected_for_non_inapp_channel(Channel channel)
    {
        var delivery = NotificationMother.PendingDelivery(channel);
        delivery.MarkSent("x");

        var result = delivery.MarkRead();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("delivery.read_not_applicable");
        delivery.State.Should().Be(DeliveryState.Sent, "read tak berlaku untuk Email/Push — state tak berubah");
    }

    [Fact]
    public void MarkRead_is_idempotent()
    {
        var delivery = NotificationMother.PendingDelivery(Channel.InApp);
        delivery.MarkSent(null);
        delivery.MarkRead();

        var again = delivery.MarkRead();

        again.IsSuccess.Should().BeTrue();
        delivery.State.Should().Be(DeliveryState.Read);
    }

    [Fact]
    public void State_transitions_raise_no_domain_events()
    {
        var delivery = NotificationMother.PendingDelivery(Channel.InApp);
        delivery.MarkSent(null);
        delivery.MarkRead();

        delivery.DomainEvents.Should().BeEmpty();
    }
}
