using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;

namespace Wms.Platform.Azure.Messaging;

// Membaca message yang masuk ke dead letter queue milik Service Bus.
// Dead letter dari pipeline aplikasi tetap disimpan lewat tabel infrastructure.dead_letter.
public sealed class ServiceBusDeadLetterStore(ServiceBusClient client, IOptions<AzureMessagingOptions> options)
{
    // Hanya untuk melihat isi DLQ, tanpa menghapus atau mengubah messagenya.
    public async Task<IReadOnlyList<DeadLetteredMessage>> PeekAsync(
        string subscriptionName,
        int maxMessages = 10,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subscriptionName);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxMessages, 0);

        var receiver = client.CreateReceiver(
            options.Value.CoreFlowTopicName,
            subscriptionName,
            new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

        await using (receiver.ConfigureAwait(false))
        {
            var messages = await receiver
                .PeekMessagesAsync(maxMessages, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return [.. messages.Select(message => new DeadLetteredMessage(
                message.MessageId,
                message.Subject,
                message.Body.ToString(),
                message.DeadLetterReason,
                message.DeadLetterErrorDescription,
                message.DeliveryCount))];
        }
    }
}

// Data Message di DLQ untuk kebutuhan inspeksi operasional.
public sealed record DeadLetteredMessage(
    string MessageId,
    string? LogicalName,
    string Payload,
    string? Reason,
    string? Description,
    int DeliveryCount);
