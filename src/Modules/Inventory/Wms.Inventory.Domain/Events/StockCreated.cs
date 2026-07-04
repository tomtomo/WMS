using Wms.BuildingBlocks.Domain.Events;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Domain.Events;

// Balance terbentuk dari GRConfirmed
public sealed record StockCreated(
    StockId StockId,
    Sku Sku,
    decimal Qty,
    StockStatus Status,
    Guid SourceGrId) : IDomainEvent;
