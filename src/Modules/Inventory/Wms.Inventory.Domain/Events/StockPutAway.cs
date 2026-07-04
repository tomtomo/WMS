using Wms.BuildingBlocks.Domain.Events;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Domain.Events;

// Balance pindah ke rak dan menjadi allocatable.
public sealed record StockPutAway(StockId StockId, LocationId LocationId) : IDomainEvent;
