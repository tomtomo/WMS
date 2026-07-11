using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Shared.Notifications;

// FCM dipakai sebagai provider push di semua cloud, jadi Azure dan GCP menggunakan adapter yang sama.
// Jika pengiriman gagal, exception diteruskan agar worker dapat melakukan retry dan memindahkannya ke dead-letter.
public sealed class FcmPushNotifier(IFirebaseMessagingClient messaging, ILogger<FcmPushNotifier> logger) : IPushNotifier
{
    public async Task PushAsync(string deviceToken, string title, string body, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var providerMessageId = await messaging
            .SendAsync(new FirebasePushMessage(deviceToken, title, body), cancellationToken)
            .ConfigureAwait(false);

        // Jika providerMessageId tidak tersedia, anggap pengiriman gagal agar diproses melalui retry.
        if (string.IsNullOrWhiteSpace(providerMessageId))
        {
            throw new InvalidOperationException("FCM tidak mengembalikan providerMessageId untuk push yang dikirim.");
        }

        logger.LogInformation(
            "Push FCM terkirim {ProviderMessageId} judul {Title}",
            providerMessageId,
            title);
    }
}
