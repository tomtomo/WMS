using Microsoft.EntityFrameworkCore;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Domain;

namespace Wms.Outbound.Infrastructure.Persistence;

// Write side OutboundOrder: tracked, commit oleh handler/consumer via IUnitOfWork.
internal sealed class OutboundOrderRepository(OutboundDbContext context) : IOutboundOrderRepository
{
    public Task AddAsync(OutboundOrder order, CancellationToken cancellationToken = default)
    {
        context.Set<OutboundOrder>().Add(order);
        return Task.CompletedTask;
    }

    public Task<OutboundOrder?> GetAsync(OutboundOrderId id, CancellationToken cancellationToken = default) =>
        context.Set<OutboundOrder>().FirstOrDefaultAsync(order => order.Id == id, cancellationToken);

    public async Task<IReadOnlyList<OutboundOrder>> ListByIdsAsync(
        IReadOnlyCollection<OutboundOrderId> ids,
        CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await context.Set<OutboundOrder>()
            .Where(order => idList.Contains(order.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OutboundOrder>> ListByWaveAsync(
        WaveId waveId,
        CancellationToken cancellationToken = default) =>
        await context.Set<OutboundOrder>()
            .Where(order => order.WaveId == waveId)
            .ToListAsync(cancellationToken);
}
