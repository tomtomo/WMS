using Wms.Contracts.Abstractions;

namespace Wms.Outbound.Contracts;

// Outbound ke Notifications: semua PickingTask wave selesai. Notification: alert SPV siap dispatch.
public sealed record WaveReady(
    Guid WaveId,
    Guid WarehouseId) : IIntegrationEvent
{
    public const string LogicalName = "outbound.wave_ready.v1";

    public const DeliveryClass DeliveryClass = DeliveryClass.Notification;
}
