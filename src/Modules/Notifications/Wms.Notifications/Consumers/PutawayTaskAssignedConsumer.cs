using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Contracts;
using Wms.Notifications.Deliveries;

namespace Wms.Notifications.Consumers;

// Membuat notifikasi langsung untuk operator yang menerima tugas putaway.
public sealed class PutawayTaskAssignedConsumer(
    IInboxGuard inbox,
    NotificationFanout fanout,
    IUnitOfWork unitOfWork)
{
    public const string HandlerType = "notifications.putaway_task_assigned";

    private static readonly Channel[] _channels = [Channel.InApp, Channel.Push];

    public async Task<Result> ConsumeAsync(
        PutawayTaskAssigned integrationEvent,
        Guid eventId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (await inbox.HasProcessedAsync(eventId, HandlerType, cancellationToken))
        {
            return Result.Success();
        }

        var content = new NotificationContent(
            "Tugas putaway baru",
            $"Putaway task SKU {integrationEvent.Sku}.",
            nameof(PutawayTaskAssigned));
        await fanout.EnqueueDirectAsync(
            integrationEvent.AssignedTo, _channels, content, integrationEvent.WarehouseId, eventId.ToString("N"), cancellationToken);

        await inbox.MarkProcessedAsync(eventId, HandlerType, cancellationToken);
        return await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
