using Microsoft.Extensions.Logging;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Local.Notifications;

// Log-stub channel email (cloud: ACS / SendGrid).
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        var providerMessageId = Guid.NewGuid().ToString("N");
        logger.LogInformation(
            "Email stub terkirim {ProviderMessageId} ke {To} subjek {Subject}",
            providerMessageId,
            to,
            subject);

        return Task.CompletedTask;
    }
}
