using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Outbound.Contracts;

namespace Wms.Notifications.Consumers;

// Mengirim notifikasi saat wave siap untuk dispatch.
public sealed class WaveReadyConsumer(
    IInboxGuard inbox,
    NotificationFanout fanout,
    IUnitOfWork unitOfWork)
{
    public const string HandlerType = "notifications.wave_ready";

    public async Task<Result> ConsumeAsync(
        WaveReady integrationEvent,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (await inbox.HasProcessedAsync(eventId, HandlerType, cancellationToken))
        {
            return Result.Success();
        }

        var content = new NotificationContent(
            "Wave siap dispatch",
            $"Wave {integrationEvent.WaveId:N} siap dispatch.",
            nameof(WaveReady));
        await fanout.FanOutAsync(
            NotificationTopics.WaveReady, content, integrationEvent.WarehouseId, eventId.ToString("N"), cancellationToken);

        await inbox.MarkProcessedAsync(eventId, HandlerType, cancellationToken);
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
