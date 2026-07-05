using Wms.Inventory.Domain;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Application.Abstractions;

// Write side StockReservation (aggregate root, lifecycle terikat wave). Commit via IUnitOfWork.
public interface IStockReservationRepository
{
    Task AddAsync(StockReservation reservation, CancellationToken cancellationToken = default);

    Task<StockReservation?> GetAsync(StockReservationId id, CancellationToken cancellationToken = default);

    // Natural key idempotent consumer (waveId, orderId, sku)
    Task<bool> ExistsForLineAsync(Guid waveId, Guid orderId, Sku sku, CancellationToken cancellationToken = default);
}
