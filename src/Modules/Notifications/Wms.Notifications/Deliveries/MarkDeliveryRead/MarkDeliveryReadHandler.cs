using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Notifications.Abstractions;

namespace Wms.Notifications.Deliveries.MarkDeliveryRead;

// Menandai notifikasi sebagai sudah dibaca.
internal sealed class MarkDeliveryReadHandler(
    INotificationDeliveryRepository repository,
    ICurrentUser currentUser)
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

        // User biasa hanya boleh melihat delivery miliknya sendiri.
        // Jika tidak punya akses, balasannya tetap not found supaya data delivery lain tidak terbuka.
        if (delivery is null || !CallerMayRead(delivery))
        {
            return Result.NotFound(new Error("delivery.not_found", "Delivery tidak ditemukan."));
        }

        return delivery.MarkRead();
    }

    private bool CallerMayRead(NotificationDelivery delivery) =>
        currentUser.CanBypassWarehouseScope
        || (Guid.TryParse(currentUser.UserId, out var callerId) && delivery.UserId == callerId);
}
