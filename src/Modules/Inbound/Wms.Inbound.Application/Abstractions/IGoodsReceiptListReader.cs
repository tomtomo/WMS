using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Inbound.Application.ReadModels;

namespace Wms.Inbound.Application.Abstractions;

// Read port antrean review SPV — GR berstatus Pending, paged.
public interface IGoodsReceiptListReader : IReader
{
    Task<PagedResult<GoodsReceiptListItemDto>> ListPendingAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
