using System.Diagnostics;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure.DeadLetter;
using Wms.Notifications.Abstractions;

namespace Wms.Notifications.Deliveries;

// Mengirim delivery notifikasi ke channel yang sesuai.
internal sealed class DeliveryDispatcher(
    INotificationDeliveryRepository repository,
    IInAppNotifier inAppNotifier,
    IEmailSender emailSender,
    IPushNotifier pushNotifier,
    IUserDirectory directory,
    ConsumerDeadLetterPipeline deadLetterPipeline,
    IUnitOfWork unitOfWork)
{
    private const string DeadLetterSource = "notifications.delivery";

    public async Task<bool> TryDispatchAsync(DeliveryId id, CancellationToken cancellationToken = default)
    {
        var delivery = await repository.GetAsync(id, cancellationToken);

        // Lewati jika delivery tidak ditemukan atau sudah tidak pending.
        if (delivery is null || delivery.State != DeliveryState.Pending)
        {
            return false;
        }

        var sent = false;
        await deadLetterPipeline.ExecuteAsync(
            DeadLetterSource,
            DescribeDelivery(delivery),
            async token =>
            {
                await SendToChannelAsync(delivery, token);
                sent = true;
            },
            cancellationToken);

        if (sent)
        {
            // Provider lokal belum mengembalikan message id.
            delivery.MarkSent(providerMessageId: null);
        }
        else
        {
            // Simpan ringkasan kegagalan. Detailnya dicatat di dead letter.
            delivery.MarkFailed(
                $"Dispatch gagal setelah {ConsumerDeadLetterPipeline.MaxAttempts} percobaan; detail di dead-letter.",
                ConsumerDeadLetterPipeline.MaxAttempts);
        }

        // Simpan perubahan status delivery.
        var save = await unitOfWork.SaveChangesAsync(cancellationToken);
        return sent && save.IsSuccess;
    }

    private static string DescribeDelivery(NotificationDelivery delivery) =>
        $"{delivery.EventType}:{delivery.Channel}:{delivery.UserId:N}";

    private async Task SendToChannelAsync(NotificationDelivery delivery, CancellationToken cancellationToken)
    {
        switch (delivery.Channel)
        {
            case Channel.InApp:
                await inAppNotifier.NotifyAsync(delivery.UserId.ToString(), delivery.Body, cancellationToken);
                break;

            case Channel.Email:
            {
                var recipient = await ResolveRecipientAsync(delivery.UserId, cancellationToken);
                await emailSender.SendAsync(recipient.Email, delivery.Title, delivery.Body, cancellationToken);
                break;
            }

            case Channel.Push:
            {
                var recipient = await ResolveRecipientAsync(delivery.UserId, cancellationToken);
                await pushNotifier.PushAsync(recipient.DeviceToken, delivery.Title, delivery.Body, cancellationToken);
                break;
            }

            default:
                throw new UnreachableException($"Channel tak dikenal: {delivery.Channel}");
        }
    }

    private async Task<NotificationRecipient> ResolveRecipientAsync(Guid userId, CancellationToken cancellationToken)
    {
        var recipient = await directory.GetRecipientAsync(userId, cancellationToken);

        // Email dan push membutuhkan data recipient.
        return recipient ?? throw new InvalidOperationException($"Recipient {userId} tak ditemukan untuk channel Email/Push.");
    }
}
