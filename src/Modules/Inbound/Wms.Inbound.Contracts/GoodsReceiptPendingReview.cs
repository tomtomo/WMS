using Wms.Contracts.Abstractions;

namespace Wms.Inbound.Contracts;

// Inbound ke Notifications: GR masuk state Pending (menunggu review SPV). Notification: fire and forget alert.
public sealed record GoodsReceiptPendingReview(
    Guid GrId,
    Guid WarehouseId,
    bool HasOverDelivery,
    int DiscrepancyCount) : IIntegrationEvent
{
    public const string LogicalName = "inbound.goods_receipt_pending_review.v1";

    public const DeliveryClass DeliveryClass = DeliveryClass.Notification;
}
