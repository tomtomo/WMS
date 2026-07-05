using Microsoft.EntityFrameworkCore;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Domain;

namespace Wms.Outbound.Infrastructure.Persistence;

// Write side PickingTask: tracked, commit oleh consumer/handler via IUnitOfWork.
internal sealed class PickingTaskRepository(OutboundDbContext context) : IPickingTaskRepository
{
    public Task AddAsync(PickingTask task, CancellationToken cancellationToken = default)
    {
        context.Set<PickingTask>().Add(task);
        return Task.CompletedTask;
    }

    public Task<PickingTask?> GetAsync(PickingTaskId id, CancellationToken cancellationToken = default) =>
        context.Set<PickingTask>().FirstOrDefaultAsync(task => task.Id == id, cancellationToken);

    // Natural key idempotent consumer (waveId, reservationId).
    public Task<bool> ExistsForReservationAsync(
        WaveId waveId,
        Guid reservationId,
        CancellationToken cancellationToken = default) =>
        context.Set<PickingTask>()
            .AnyAsync(task => task.WaveId == waveId && task.ReservationId == reservationId, cancellationToken);

    public async Task<IReadOnlyList<PickingTask>> ListByWaveAsync(
        WaveId waveId,
        CancellationToken cancellationToken = default) =>
        await context.Set<PickingTask>()
            .Where(task => task.WaveId == waveId)
            .ToListAsync(cancellationToken);
}
