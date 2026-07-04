using Microsoft.EntityFrameworkCore;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Domain;

namespace Wms.Inventory.Infrastructure.Persistence;

// Write side Stock: tracked, commit oleh consumer/handler via IUnitOfWork.
internal sealed class StockRepository(InventoryDbContext context) : IStockRepository
{
    public Task AddAsync(Stock stock, CancellationToken cancellationToken = default)
    {
        context.Set<Stock>().Add(stock);
        return Task.CompletedTask;
    }

    public Task<Stock?> GetAsync(StockId id, CancellationToken cancellationToken = default) =>
        context.Set<Stock>().FirstOrDefaultAsync(stock => stock.Id == id, cancellationToken);

    // Natural key idempotent consumer.
    public Task<bool> ExistsForReceiptLineAsync(Guid sourceGrId, int line, CancellationToken cancellationToken = default) =>
        context.Set<Stock>().AnyAsync(stock => stock.SourceGrId == sourceGrId && stock.Line == line, cancellationToken);
}
