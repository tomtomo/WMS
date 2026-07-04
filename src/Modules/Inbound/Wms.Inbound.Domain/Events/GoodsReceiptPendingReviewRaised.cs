using Wms.BuildingBlocks.Domain.Events;

namespace Wms.Inbound.Domain.Events;

// GR menunggu review SPV
public sealed record GoodsReceiptPendingReviewRaised(
    GoodsReceiptId GoodsReceiptId,
    Guid WarehouseId,
    bool HasOverDelivery,
    int DiscrepancyCount) : IDomainEvent;
