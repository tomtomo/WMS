using FirebaseAdmin.Messaging;

namespace Wms.Platform.Shared.Notifications;

// Adapter yang meneruskan message ke Firebase Admin SDK. instance FirebaseMessaging disiapkan di composition root tiap cloud.
public sealed class FirebaseAdminMessagingClient(FirebaseMessaging messaging) : IFirebaseMessagingClient
{
    public Task<string> SendAsync(FirebasePushMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Port membawa registration token perangkat, jadi field Token tetap digunakan meski SDK menandainya deprecated.
#pragma warning disable CS0618
        var fcmMessage = new Message
        {
            Token = message.DeviceToken,
            Notification = new Notification { Title = message.Title, Body = message.Body },
        };
#pragma warning restore CS0618

        return messaging.SendAsync(fcmMessage, cancellationToken);
    }
}
