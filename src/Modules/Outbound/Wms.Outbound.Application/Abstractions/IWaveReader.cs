using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Outbound.Application.ReadModels;

namespace Wms.Outbound.Application.Abstractions;

// Read port Wave detail — AsNoTracking, langsung ke read DTO tanpa aggregate.
public interface IWaveReader : IReader
{
    Task<WaveDto?> GetByIdAsync(Guid waveId, CancellationToken cancellationToken = default);
}
