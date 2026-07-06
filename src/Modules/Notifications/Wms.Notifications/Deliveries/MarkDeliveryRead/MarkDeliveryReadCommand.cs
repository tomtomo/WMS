using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Notifications.Deliveries.MarkDeliveryRead;

// User menandai notifikasi sudah dibaca
public sealed record MarkDeliveryReadCommand(Guid DeliveryId) : ICommand;
