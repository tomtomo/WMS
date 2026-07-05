using Microsoft.EntityFrameworkCore;
using Wms.Outbound.Application.Abstractions;
using Wms.Outbound.Application.ReadModels;
using Wms.Outbound.Domain;
using Wms.Outbound.Domain.Enums;

namespace Wms.Outbound.Infrastructure.Persistence;

// Read port Wave — AsNoTracking, map ke DTO tanpa AutoMapper.
internal sealed class WaveReader(OutboundDbContext context) : IWaveReader, IWaveListReader
{
    public async Task<WaveDto?> GetByIdAsync(Guid waveId, CancellationToken cancellationToken = default)
    {
        var id = WaveId.Create(waveId);
        if (id.IsFailure)
        {
            return null;
        }

        var wave = await context.Set<Wave>().AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == id.Value, cancellationToken);
        if (wave is null)
        {
            return null;
        }

        var tasks = await context.Set<PickingTask>().AsNoTracking()
            .Where(task => task.WaveId == id.Value)
            .ToListAsync(cancellationToken);

        return new WaveDto(
            wave.Id.Value,
            wave.WarehouseId,
            wave.Status.ToString(),
            wave.CancelReason?.Value,
            [.. wave.OrderIds.Select(orderId => orderId.Value)],
            tasks.Count,
            tasks.Count(task => task.Status == PickingTaskStatus.Completed));
    }

    public async Task<IReadOnlyList<WaveListItemDto>> GetByStatusAsync(
        Guid warehouseId,
        string status,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<WaveStatus>(status, ignoreCase: true, out var waveStatus))
        {
            return [];
        }

        var waves = await context.Set<Wave>().AsNoTracking()
            .Where(wave => wave.WarehouseId == warehouseId && wave.Status == waveStatus)
            .ToListAsync(cancellationToken);

        return [.. waves.Select(wave => new WaveListItemDto(
            wave.Id.Value,
            wave.WarehouseId,
            wave.Status.ToString(),
            wave.OrderIds.Count))];
    }
}
