using Wms.Inbound.Domain;

namespace Wms.Inbound.Application.Abstractions;

// Write side GoodsReceipt, commit oleh TransactionBehavior via IUnitOfWork.
public interface IGoodsReceiptRepository
{
    Task AddAsync(GoodsReceipt goodsReceipt, CancellationToken cancellationToken = default);

    Task<GoodsReceipt?> GetAsync(GoodsReceiptId id, CancellationToken cancellationToken = default);
}
