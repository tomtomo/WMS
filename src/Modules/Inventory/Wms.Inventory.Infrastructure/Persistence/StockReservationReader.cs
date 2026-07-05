using Microsoft.EntityFrameworkCore;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Application.ReadModels;
using Wms.Inventory.Domain;

namespace Wms.Inventory.Infrastructure.Persistence;

// Read port reservasi — AsNoTracking, map ke DTO tanpa AutoMapper.
internal sealed class StockReservationReader(InventoryDbContext context) : IStockReservationReader
{
    public async Task<IReadOnlyList<ReservationDto>> GetByWaveAsync(
        Guid waveId,
        CancellationToken cancellationToken = default)
    {
        var reservations = await context.Set<StockReservation>().AsNoTracking()
            .Where(reservation => reservation.WaveId == waveId)
            .ToListAsync(cancellationToken);
        return [.. reservations.Select(Map)];
    }

    private static ReservationDto Map(StockReservation reservation) => new(
        reservation.Id.Value,
        reservation.StockId.Value,
        reservation.WaveId,
        reservation.OrderId,
        reservation.Sku.Value,
        reservation.Batch.Value,
        reservation.Qty,
        reservation.Status.ToString(),
        reservation.PickingTaskId);
}
