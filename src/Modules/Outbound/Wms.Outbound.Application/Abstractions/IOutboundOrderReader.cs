using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Outbound.Application.ReadModels;

namespace Wms.Outbound.Application.Abstractions;

// Read port OutboundOrder — backlog dan detail per line allocationStatus.
public interface IOutboundOrderReader : IReader
{
    // Order backlog (New — belum/tidak lagi di wave).
    Task<IReadOnlyList<OutboundOrderDto>> GetBacklogAsync(CancellationToken cancellationToken = default);

    Task<OutboundOrderDto?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}
