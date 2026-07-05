using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Inventory.Application.ReadModels;

namespace Wms.Inventory.Application.Abstractions;

// Read port balance — AsNoTracking, langsung ke read DTO tanpa aggregate.
public interface IStockReader : IReader
{
    // Balance Available (allocatable) per warehouse, opsional filter SKU.
    Task<IReadOnlyList<AvailableStockView>> GetAvailableAsync(
        Guid warehouseId,
        string? sku,
        CancellationToken cancellationToken = default);

    // Lookup satu balance by id — sync read lintas modul (gRPC).
    Task<AvailableStockView?> GetByIdAsync(Guid stockId, CancellationToken cancellationToken = default);

    // Balance Available/OnHand dengan expiry ≤ threshold (untuk StockNearExpiry). Bukan transisi state.
    Task<IReadOnlyList<AvailableStockView>> GetExpiringAsync(DateOnly threshold, CancellationToken cancellationToken = default);
}
