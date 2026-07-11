using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.Platform.Azure.Notifications;

// Pengiriman email di Azure menggunakan Azure Communication Services, menggantikan sender berbasis log di Local.
// Jika pengiriman gagal, exception diteruskan agar worker dapat melakukan retry dan memindahkannya ke dead-letter.
public sealed class AcsEmailSender(
    EmailClient client,
    IOptions<AcsEmailOptions> options,
    ILogger<AcsEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(to);
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        // Kirim body sebagai plain text karena isinya berasal dari data domain, bukan HTML yang sudah dipercaya.
        var message = new EmailMessage(
            options.Value.SenderAddress,
            to,
            new EmailContent(subject) { PlainText = body });

        // Cukup tunggu sampai ACS menerima permintaan pengiriman, tanpa menunggu proses email selesai.
        var operation = await client.SendAsync(WaitUntil.Started, message, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(operation.Id))
        {
            throw new InvalidOperationException("ACS tidak mengembalikan providerMessageId untuk email yang dikirim.");
        }

        logger.LogInformation("Email ACS terkirim {ProviderMessageId} subjek {Subject}", operation.Id, subject);
    }
}
