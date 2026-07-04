using Wms.BuildingBlocks.Application.ReadModels;
using Wms.Inbound.Application.ReadModels;

namespace Wms.Inbound.Application.Abstractions;

// Read port metadata attachment GR.
public interface IGRAttachmentReader : IReader
{
    // Default hanya yang aktif
    Task<IReadOnlyList<GRAttachmentDto>> ListByGoodsReceiptAsync(
        Guid goodsReceiptId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default);

    // ContentRef attachment aktif
    Task<string?> GetActiveContentRefAsync(
        Guid goodsReceiptId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);
}
