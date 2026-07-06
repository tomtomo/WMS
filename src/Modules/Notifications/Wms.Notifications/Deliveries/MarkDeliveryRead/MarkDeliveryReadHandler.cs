using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Notifications.Abstractions;

namespace Wms.Notifications.Deliveries.MarkDeliveryRead;

// Menandai notifikasi sebagai sudah dibaca.
internal sealed class MarkDeliveryReadHandler(INotificationDeliveryRepository repository)
    : ICommandHandler<MarkDeliveryReadCommand>
{
    public async Task<Result> Handle(MarkDeliveryReadCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = DeliveryId.Create(command.DeliveryId);
        if (id.IsFailure)
        {
            return Result.Invalid(id.Error);
        }

        var delivery = await repository.GetAsync(id.Value, cancellationToken);
        if (delivery is null)
        {
            return Result.NotFound(new Error("delivery.not_found", "Delivery tak ditemukan."));
        }

        return delivery.MarkRead();
    }
}
