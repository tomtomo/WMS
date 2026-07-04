using Wms.BuildingBlocks.Domain.Events;
using Wms.Inbound.Domain.ValueObjects;

namespace Wms.Inbound.Domain.Events;

// Stok siap diserahkan ke Inventory
public sealed record GoodsReceiptConfirmed(
    GoodsReceiptId GoodsReceiptId,
    Guid WarehouseId,
    Guid SupplierId,
    IReadOnlyList<ReceivedLine> ReceivedLines,
    IReadOnlyList<RejectedLine> RejectedLines) : IDomainEvent;
