using Wms.Inventory.Contracts;
using Wms.Inventory.Contracts.Enums;
using Wms.Inventory.Contracts.Payloads;

namespace Wms.Outbound.IntegrationTests.TestSupport;

// Builder outcome StockAllocationCompleted (dari Inventory) untuk skenario consumer Outbound.
internal static class StockAllocationCompletedFactory
{
    public static Allocation AllocationOf(Guid orderId, string sku, decimal qty, Guid reservationId, string? batch = "LOT-1") =>
        new(orderId, sku, LocationId: Guid.NewGuid(), batch, qty, StockId: Guid.NewGuid(), reservationId);

    public static Shortfall ShortfallOf(Guid orderId, string sku, decimal requestedQty, decimal allocatedQty) =>
        new(orderId, sku, requestedQty, allocatedQty, requestedQty - allocatedQty);

    public static StockAllocationCompleted FullyAllocated(Guid waveId, params Allocation[] allocations) =>
        new(waveId, AllocationStatus.FullyAllocated, allocations, []);

    public static StockAllocationCompleted PartiallyAllocated(
        Guid waveId,
        IReadOnlyList<Allocation> allocations,
        IReadOnlyList<Shortfall> shortfalls) =>
        new(waveId, AllocationStatus.PartiallyAllocated, allocations, shortfalls);

    public static StockAllocationCompleted Unfulfilled(Guid waveId, params Shortfall[] shortfalls) =>
        new(waveId, AllocationStatus.Unfulfilled, [], shortfalls);
}
