using Microsoft.EntityFrameworkCore;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Domain;

namespace Wms.Outbound.Infrastructure.Persistence;

// Write side Wave: tracked, commit oleh handler/consumer via IUnitOfWork.
internal sealed class WaveRepository(OutboundDbContext context) : IWaveRepository
{
    public Task AddAsync(Wave wave, CancellationToken cancellationToken = default)
    {
        context.Set<Wave>().Add(wave);
        return Task.CompletedTask;
    }

    public Task<Wave?> GetAsync(WaveId id, CancellationToken cancellationToken = default) =>
        context.Set<Wave>().FirstOrDefaultAsync(wave => wave.Id == id, cancellationToken);
}
