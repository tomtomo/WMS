using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Infrastructure.Messaging;

namespace Wms.BuildingBlocks.Infrastructure.Outbox;

// Base dispatcher: route satu envelope ke rail sesuai DeliveryClass lalu throw jika publish gagal.
public abstract class OutboxDispatcher
{
    public const int BatchSize = 50;

    public static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    public async Task DispatchAsync(MessageEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        var result = envelope.DeliveryClass switch
        {
            DeliveryClass.CoreFlow => await PublishToCoreFlowAsync(envelope, cancellationToken),
            DeliveryClass.Notification => await PublishToNotificationAsync(envelope, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(
                nameof(envelope),
                envelope.DeliveryClass,
                "DeliveryClass tak dikenal."),
        };

        if (result.IsFailure)
        {
            throw new OutboxDispatchException(
                $"Publish Outbox gagal untuk '{envelope.LogicalName}': {result.Error.Code}.");
        }
    }

    protected abstract Task<Result> PublishToCoreFlowAsync(
        MessageEnvelope envelope,
        CancellationToken cancellationToken);

    protected abstract Task<Result> PublishToNotificationAsync(
        MessageEnvelope envelope,
        CancellationToken cancellationToken);
}
