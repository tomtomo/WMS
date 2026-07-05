using Microsoft.EntityFrameworkCore;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Infrastructure.Persistence;

// Write side StockReservation: tracked, commit oleh consumer/handler via IUnitOfWork.
internal sealed class StockReservationRepository(InventoryDbContext context) : IStockReservationRepository
{
    public Task AddAsync(StockReservation reservation, CancellationToken cancellationToken = default)
    {
        context.Set<StockReservation>().Add(reservation);
        return Task.CompletedTask;
    }

    public Task<StockReservation?> GetAsync(StockReservationId id, CancellationToken cancellationToken = default) =>
        context.Set<StockReservation>().FirstOrDefaultAsync(reservation => reservation.Id == id, cancellationToken);

    // Natural key idempotent consumer (waveId, orderId, sku)
    public Task<bool> ExistsForLineAsync(
        Guid waveId,
        Guid orderId,
        Sku sku,
        CancellationToken cancellationToken = default) =>
        context.Set<StockReservation>()
            .AnyAsync(
                reservation => reservation.WaveId == waveId && reservation.OrderId == orderId && reservation.Sku == sku,
                cancellationToken);
}
