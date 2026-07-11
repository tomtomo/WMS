using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Shared.Notifications;

// Kirim notifikasi ke semua koneksi milik user agar seluruh tab atau circuit menerima message yang sama.
// Jika pengiriman gagal, exception diteruskan agar worker yang menangani retry.
public sealed class SignalRInAppNotifier(
    IHubContext<NotificationHub> hubContext,
    ILogger<SignalRInAppNotifier> logger) : IInAppNotifier
{
    public async Task NotifyAsync(string userId, string message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        await hubContext.Clients
            .User(userId)
            .SendAsync(NotificationHub.ReceiveNotificationMethod, message, cancellationToken)
            .ConfigureAwait(false);

        logger.LogInformation("Notifikasi in-app terkirim ke user {UserId}", userId);
    }
}
