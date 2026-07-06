using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Notifications.Deliveries;
using Wms.Outbound.Contracts;

namespace Wms.Notifications.Consumers;

// Mengirim notifikasi saat tugas picking diberikan ke operator.
public sealed class PickingTaskAssignedConsumer(
    IInboxGuard inbox,
    NotificationFanout fanout,
    IUnitOfWork unitOfWork)
{
    public const string HandlerType = "notifications.picking_task_assigned";

    private static readonly Channel[] _channels = [Channel.InApp, Channel.Push];

    public async Task<Result> ConsumeAsync(
        PickingTaskAssigned integrationEvent,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (await inbox.HasProcessedAsync(eventId, HandlerType, cancellationToken))
        {
            return Result.Success();
        }

        var content = new NotificationContent(
            "Tugas picking baru",
            $"Picking task SKU {integrationEvent.Sku}.",
            nameof(PickingTaskAssigned));
        await fanout.EnqueueDirectAsync(
            integrationEvent.AssignedTo, _channels, content, integrationEvent.WarehouseId, eventId.ToString("N"), cancellationToken);

        await inbox.MarkProcessedAsync(eventId, HandlerType, cancellationToken);
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
