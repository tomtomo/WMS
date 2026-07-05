using Wms.Inventory.Domain;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Application.Abstractions;

// Write side Stock, commit oleh consumer/handler via IUnitOfWork.
public interface IStockRepository
{
    Task AddAsync(Stock stock, CancellationToken cancellationToken = default);

    Task<Stock?> GetAsync(StockId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsForReceiptLineAsync(Guid sourceGrId, int line, CancellationToken cancellationToken = default);

    // Kandidat alokasi FEFO (Available, urut expiry terdekat) — tracked
    Task<IReadOnlyList<Stock>> GetAllocatableAsync(Sku sku, CancellationToken cancellationToken = default);

    // Balance Picked terikat wave — dihapus saat ShipmentDispatched (tracked).
    Task<IReadOnlyList<Stock>> GetPickedByWaveAsync(Guid waveId, CancellationToken cancellationToken = default);

    void Remove(Stock stock);
}
