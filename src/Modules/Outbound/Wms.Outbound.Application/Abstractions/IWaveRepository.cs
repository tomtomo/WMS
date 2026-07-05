using Wms.Outbound.Domain;

namespace Wms.Outbound.Application.Abstractions;

// Write side Wave (aggregate root). Commit via IUnitOfWork.
public interface IWaveRepository
{
    Task AddAsync(Wave wave, CancellationToken cancellationToken = default);

    Task<Wave?> GetAsync(WaveId id, CancellationToken cancellationToken = default);
}
