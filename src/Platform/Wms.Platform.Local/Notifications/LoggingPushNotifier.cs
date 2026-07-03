using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Notifications;

// Log-stub channel push (cloud: FCM lintas-cloud).
public sealed class LoggingPushNotifier(ILogger<LoggingPushNotifier> logger) : IPushNotifier
{
    public Task PushAsync(string deviceToken, string title, string body, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        var providerMessageId = Guid.NewGuid().ToString("N");
        logger.LogInformation(
            "Push stub terkirim {ProviderMessageId} ke device {DeviceToken} judul {Title}",
            providerMessageId,
            deviceToken,
            title);

        return Task.CompletedTask;
    }
}
