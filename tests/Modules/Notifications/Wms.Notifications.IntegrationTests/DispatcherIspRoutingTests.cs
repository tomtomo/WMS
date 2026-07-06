using AwesomeAssertions;
using NSubstitute;
using Wms.Notifications.Consumers;
using Wms.Notifications.Deliveries;
using Wms.Notifications.IntegrationTests.TestSupport;
using Wms.Notifications.Subscriptions;
using Xunit;

namespace Wms.Notifications.IntegrationTests;

// Test routing dispatcher berdasarkan channel.
[Collection(PostgresCollection.Name)]
public sealed class DispatcherIspRoutingTests(PostgresFixture postgres) : NotificationsTestBase(postgres)
{
    [Fact]
    public async Task Each_channel_routed_to_its_own_isp_port_exactly_once()
    {
        var inAppUser = Guid.NewGuid();
        var emailUser = Guid.NewGuid();
        var pushUser = Guid.NewGuid();
        await SeedChannelSubscriber(inAppUser, Channel.InApp);
        await SeedChannelSubscriber(emailUser, Channel.Email);
        await SeedChannelSubscriber(pushUser, Channel.Push);

        await DeliverAsync<WaveReadyConsumer>(consumer =>
            consumer.ConsumeAsync(SampleEvents.WaveReady(Guid.NewGuid()), Guid.NewGuid()));

        (await DispatchAsync()).Should().Be(3);

        // Tiap port menerima tepat satu panggilan dengan recipient yang benar
        await InAppNotifier.Received(1).NotifyAsync(inAppUser.ToString(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await EmailSender.Received(1).SendAsync($"{emailUser:N}@wms.local", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await PushNotifier.Received(1).PushAsync($"device-{pushUser:N}", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        await InAppNotifier.Received(1).NotifyAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await EmailSender.Received(1).SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await PushNotifier.Received(1).PushAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());

        (await AllDeliveriesAsync()).Should().OnlyContain(delivery => delivery.State == DeliveryState.Sent);
    }

    private async Task SeedChannelSubscriber(Guid userId, Channel channel)
    {
        var role = Guid.NewGuid();
        Directory.SetRoleMembers(role, userId);
        await SeedSubscriptionAsync(SubscriberType.Role, role, NotificationTopics.WaveReady, [channel]);
    }
}
