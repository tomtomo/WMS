using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Notifications;

// Log-stub channel in-app (cloud: self-hosted SignalR).
public sealed class LoggingInAppNotifier(ILogger<LoggingInAppNotifier> logger) : IInAppNotifier
{
    public Task NotifyAsync(string userId, string message, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        var providerMessageId = Guid.NewGuid().ToString("N");
        logger.LogInformation(
            "Notifikasi in-app stub terkirim {ProviderMessageId} ke user {UserId}",
            providerMessageId,
            userId);

        return Task.CompletedTask;
    }
}
