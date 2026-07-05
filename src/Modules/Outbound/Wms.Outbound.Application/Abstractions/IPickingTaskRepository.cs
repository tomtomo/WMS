using Wms.Outbound.Domain;

namespace Wms.Outbound.Application.Abstractions;

// Write side PickingTask (aggregate root, satu per allocation). Commit via IUnitOfWork.
public interface IPickingTaskRepository
{
    Task AddAsync(PickingTask task, CancellationToken cancellationToken = default);

    Task<PickingTask?> GetAsync(PickingTaskId id, CancellationToken cancellationToken = default);

    // Idempotent create natural key (waveId, reservationId)
    Task<bool> ExistsForReservationAsync(WaveId waveId, Guid reservationId, CancellationToken cancellationToken = default);

    // Task dalam satu wave
    Task<IReadOnlyList<PickingTask>> ListByWaveAsync(WaveId waveId, CancellationToken cancellationToken = default);
}
