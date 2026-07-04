using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Inventory.Contracts;

// Inventory ke Notifications: dipicu scheduled detector (bukan transisi state) saat expiry <= threshold.
public sealed record StockNearExpiry(
    Guid StockId,
    string Sku,
    Guid WarehouseId,
    Guid LocationId,
    string Batch,
    DateOnly Expiry,
    decimal Qty,
    int DaysToExpiry) : IIntegrationEvent
{
    public const string LogicalName = "inventory.stock_near_expiry.v1";

    public const DeliveryClass DeliveryClass = DeliveryClass.Notification;
}
