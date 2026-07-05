using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Inventory.Application.ReadModels;

namespace Wms.Inventory.Application.Abstractions;

// Read port reservasi — AsNoTracking, langsung ke read DTO tanpa aggregate.
public interface IStockReservationReader : IReader
{
    // Reservasi per wave (hasil alokasi WaveReleased).
    Task<IReadOnlyList<ReservationDto>> GetByWaveAsync(Guid waveId, CancellationToken cancellationToken = default);
}
