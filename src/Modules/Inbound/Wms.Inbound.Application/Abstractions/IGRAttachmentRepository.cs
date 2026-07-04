using Wms.Inbound.Domain;

namespace Wms.Inbound.Application.Abstractions;

// Write side GRAttachment — repository sendiri karena aggregate root terpisah dari GoodsReceipt.
public interface IGRAttachmentRepository
{
    Task AddAsync(GRAttachment attachment, CancellationToken cancellationToken = default);

    // Hanya attachment aktif
    Task<GRAttachment?> GetActiveAsync(GRAttachmentId id, CancellationToken cancellationToken = default);
}
