using AwesomeAssertions;
using NSubstitute;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.Notifications.Abstractions;
using Wms.Notifications.Deliveries;
using Wms.Notifications.Deliveries.MarkDeliveryRead;
using Wms.Notifications.UnitTests.TestData;
using Xunit;

namespace Wms.Notifications.UnitTests;

// Mark as read hanya boleh untuk pemilik delivery. Actor dengan bypass scope, seperti SYSTEM atau admin, boleh lintas user.
public sealed class MarkDeliveryReadHandlerTests
{
    [Fact]
    public async Task Owner_can_mark_their_own_delivery_read()
    {
        var ownerId = Guid.NewGuid();
        var delivery = NotificationMother.PendingDelivery(Channel.InApp, userId: ownerId);
        var handler = BuildHandler(delivery, new StubCurrentUser(ownerId, canBypassScope: false));

        var result = await handler.Handle(new MarkDeliveryReadCommand(delivery.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        delivery.State.Should().Be(DeliveryState.Read);
    }

    [Fact]
    public async Task A_different_user_cannot_mark_someone_elses_delivery_read()
    {
        var delivery = NotificationMother.PendingDelivery(Channel.InApp, userId: Guid.NewGuid());
        var handler = BuildHandler(delivery, new StubCurrentUser(Guid.NewGuid(), canBypassScope: false));

        var result = await handler.Handle(new MarkDeliveryReadCommand(delivery.Id.Value), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("delivery.not_found", "delivery user lain disamarkan not-found (anti-enumeration)");
        delivery.State.Should().Be(DeliveryState.Pending, "delivery milik orang lain tak boleh berubah");
    }

    [Fact]
    public async Task A_scope_bypassing_actor_may_mark_any_delivery_read()
    {
        var delivery = NotificationMother.PendingDelivery(Channel.InApp, userId: Guid.NewGuid());
        var handler = BuildHandler(delivery, new StubCurrentUser(Guid.NewGuid(), canBypassScope: true));

        var result = await handler.Handle(new MarkDeliveryReadCommand(delivery.Id.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        delivery.State.Should().Be(DeliveryState.Read);
    }

    private static MarkDeliveryReadHandler BuildHandler(NotificationDelivery delivery, ICurrentUser currentUser)
    {
        var repository = Substitute.For<INotificationDeliveryRepository>();
        repository.GetAsync(Arg.Any<DeliveryId>(), Arg.Any<CancellationToken>()).Returns(delivery);
        return new MarkDeliveryReadHandler(repository, currentUser);
    }

    // Dibuat manual supaya CanBypassWarehouseScope bisa dikontrol langsung di test.
    private sealed class StubCurrentUser(Guid userId, bool canBypassScope) : ICurrentUser
    {
        public string UserId { get; } = userId.ToString();

        public bool IsAuthenticated => true;

        public bool CanBypassWarehouseScope { get; } = canBypassScope;
    }
}
