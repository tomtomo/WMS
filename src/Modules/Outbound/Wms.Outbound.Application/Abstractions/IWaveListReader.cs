using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Outbound.Application.ReadModels;

namespace Wms.Outbound.Application.Abstractions;

// Read port antrean wave per status (Active/Ready) di satu warehouse.
public interface IWaveListReader : IReader
{
    Task<IReadOnlyList<WaveListItemDto>> GetByStatusAsync(
        Guid warehouseId,
        string status,
        CancellationToken cancellationToken = default);
}
