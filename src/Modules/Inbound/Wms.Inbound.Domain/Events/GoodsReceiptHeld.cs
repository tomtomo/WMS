using Wms.BuildingBlocks.Domain.Events;

namespace Wms.Inbound.Domain.Events;

// GR ditolak. Inbound tidak merilis integration event untuk Hold.
public sealed record GoodsReceiptHeld(GoodsReceiptId GoodsReceiptId, Guid WarehouseId, string Reason) : IDomainEvent;
