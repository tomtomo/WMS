using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Outbound.Application.Features.CreateWave;

// SPV merilis wave dari beberapa order backlog di satu warehouse: order New ke InProgress, Wave Active,
// emit WaveReleased. Balik waveId baru.
[RequiresPermission(OutboundPermissions.CreateWave)]
public sealed record CreateWaveCommand(IReadOnlyList<Guid> OrderIds, Guid WarehouseId) : ICommand<Guid>;
