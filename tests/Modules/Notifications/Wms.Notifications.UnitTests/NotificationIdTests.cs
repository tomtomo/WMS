using AwesomeAssertions;
using Wms.Notifications.Deliveries;
using Wms.Notifications.Subscriptions;
using Xunit;

namespace Wms.Notifications.UnitTests;

public sealed class NotificationIdTests
{
    [Fact]
    public void SubscriptionId_rejects_empty_guid()
    {
        SubscriptionId.Create(Guid.Empty).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void DeliveryId_rejects_empty_guid()
    {
        DeliveryId.Create(Guid.Empty).IsFailure.Should().BeTrue();
    }

    [Fact]
    public void SubscriptionId_equality_by_value()
    {
        var guid = Guid.NewGuid();
        SubscriptionId.Create(guid).Value.Should().Be(SubscriptionId.Create(guid).Value);
    }

    [Fact]
    public void DeliveryId_equality_by_value()
    {
        var guid = Guid.NewGuid();
        DeliveryId.Create(guid).Value.Should().Be(DeliveryId.Create(guid).Value);
    }
}
