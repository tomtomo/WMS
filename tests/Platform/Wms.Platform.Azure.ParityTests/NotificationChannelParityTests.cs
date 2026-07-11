using AwesomeAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Wms.Platform.Shared.Notifications;
using Xunit;

namespace Wms.Platform.Azure.ParityTests;

// Adapter push dan in-app dipakai lintas cloud, jadi ditempatkan di Platform.Shared.
// Jika provider gagal, exception diteruskan agar worker dapat melakukan retry dan dead-letter tanpa masuk ke core.
public sealed class NotificationChannelParityTests
{
    private const string DeviceToken = "device-token-abc";
    private const string UserId = "8f1b6c2e-0000-4000-8000-000000000001";

    [Fact]
    public async Task Fcm_push_sends_the_notification_to_the_device_token()
    {
        var messaging = Substitute.For<IFirebaseMessagingClient>();
        messaging.SendAsync(Arg.Any<FirebasePushMessage>(), Arg.Any<CancellationToken>()).Returns("projects/wms/messages/42");
        var notifier = new FcmPushNotifier(messaging, NullLogger<FcmPushNotifier>.Instance);

        await notifier.PushAsync(DeviceToken, "GR dikonfirmasi", "GR-001 selesai diterima");

        await messaging.Received(1).SendAsync(
            Arg.Is<FirebasePushMessage>(message =>
                message.DeviceToken == DeviceToken
                && message.Title == "GR dikonfirmasi"
                && message.Body == "GR-001 selesai diterima"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Fcm_push_fails_when_the_provider_returns_no_message_id()
    {
        var messaging = Substitute.For<IFirebaseMessagingClient>();
        messaging.SendAsync(Arg.Any<FirebasePushMessage>(), Arg.Any<CancellationToken>()).Returns(" ");
        var notifier = new FcmPushNotifier(messaging, NullLogger<FcmPushNotifier>.Instance);

        var push = () => notifier.PushAsync(DeviceToken, "Judul", "Isi");

        // Tanpa providerMessageId kita tidak punya bukti pengiriman, jadi diperlakukan sebagai gagal agar di retry.
        await push.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Fcm_push_propagates_provider_failure_so_the_worker_can_retry()
    {
        var messaging = Substitute.For<IFirebaseMessagingClient>();
        messaging.SendAsync(Arg.Any<FirebasePushMessage>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("FCM 503"));
        var notifier = new FcmPushNotifier(messaging, NullLogger<FcmPushNotifier>.Instance);

        var push = () => notifier.PushAsync(DeviceToken, "Judul", "Isi");

        await push.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task Fcm_push_rejects_a_blank_device_token()
    {
        var notifier = new FcmPushNotifier(Substitute.For<IFirebaseMessagingClient>(), NullLogger<FcmPushNotifier>.Instance);

        var push = () => notifier.PushAsync(" ", "Judul", "Isi");

        await push.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task In_app_notification_is_pushed_to_the_user_circuit()
    {
        var clientProxy = Substitute.For<IClientProxy>();
        var hubContext = HubContextFor(clientProxy);
        var notifier = new SignalRInAppNotifier(hubContext, NullLogger<SignalRInAppNotifier>.Instance);

        await notifier.NotifyAsync(UserId, "GR-001 selesai diterima");

        await clientProxy.Received(1).SendCoreAsync(
            NotificationHub.ReceiveNotificationMethod,
            Arg.Is<object?[]>(args => (string)args[0]! == "GR-001 selesai diterima"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task In_app_notification_propagates_hub_failure_so_the_worker_can_retry()
    {
        var clientProxy = Substitute.For<IClientProxy>();
        clientProxy.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("circuit putus"));
        var notifier = new SignalRInAppNotifier(HubContextFor(clientProxy), NullLogger<SignalRInAppNotifier>.Instance);

        var notify = () => notifier.NotifyAsync(UserId, "pesan");

        await notify.Should().ThrowAsync<IOException>();
    }

    [Fact]
    public async Task In_app_notification_rejects_a_blank_user_id()
    {
        var notifier = new SignalRInAppNotifier(
            HubContextFor(Substitute.For<IClientProxy>()),
            NullLogger<SignalRInAppNotifier>.Instance);

        var notify = () => notifier.NotifyAsync(" ", "pesan");

        await notify.Should().ThrowAsync<ArgumentException>();
    }

    private static IHubContext<NotificationHub> HubContextFor(IClientProxy clientProxy)
    {
        var clients = Substitute.For<IHubClients>();
        clients.User(Arg.Any<string>()).Returns(clientProxy);
        var hubContext = Substitute.For<IHubContext<NotificationHub>>();
        hubContext.Clients.Returns(clients);
        return hubContext;
    }
}
