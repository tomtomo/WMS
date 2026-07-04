using Microsoft.EntityFrameworkCore;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Domain;

namespace Wms.Inbound.Infrastructure.Persistence;

// Write side: tracked, commit oleh TransactionBehavior.
internal sealed class GoodsReceiptRepository(InboundDbContext context) : IGoodsReceiptRepository
{
    public Task AddAsync(GoodsReceipt goodsReceipt, CancellationToken cancellationToken = default)
    {
        context.Set<GoodsReceipt>().Add(goodsReceipt);
        return Task.CompletedTask;
    }

    public Task<GoodsReceipt?> GetAsync(GoodsReceiptId id, CancellationToken cancellationToken = default) =>
        context.Set<GoodsReceipt>().FirstOrDefaultAsync(gr => gr.Id == id, cancellationToken);
}
