using Wms.Outbound.Domain;

namespace Wms.Outbound.Application.Abstractions;

// Write side OutboundOrder (aggregate root). Commit via IUnitOfWork.
public interface IOutboundOrderRepository
{
    Task AddAsync(OutboundOrder order, CancellationToken cancellationToken = default);

    Task<OutboundOrder?> GetAsync(OutboundOrderId id, CancellationToken cancellationToken = default);

    // Order terpilih saat CreateWave.
    Task<IReadOnlyList<OutboundOrder>> ListByIdsAsync(
        IReadOnlyCollection<OutboundOrderId> ids,
        CancellationToken cancellationToken = default);

    // Order dalam satu wave
    Task<IReadOnlyList<OutboundOrder>> ListByWaveAsync(WaveId waveId, CancellationToken cancellationToken = default);
}
