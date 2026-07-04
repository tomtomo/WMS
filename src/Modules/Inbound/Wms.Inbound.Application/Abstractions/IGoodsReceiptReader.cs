using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Inbound.Application.ReadModels;

namespace Wms.Inbound.Application.Abstractions;

// Read port detail GR
public interface IGoodsReceiptReader : IReader
{
    // Cek eksistensi tanpa load aggregate
    Task<bool> ExistsAsync(Guid goodsReceiptId, CancellationToken cancellationToken = default);

    Task<GoodsReceiptDto?> GetDetailAsync(Guid goodsReceiptId, CancellationToken cancellationToken = default);

    // Review SPV
    Task<GoodsReceiptReviewDto?> GetReviewAsync(Guid goodsReceiptId, CancellationToken cancellationToken = default);
}
