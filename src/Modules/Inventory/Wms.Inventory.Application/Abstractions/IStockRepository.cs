using Wms.Inventory.Domain;

namespace Wms.Inventory.Application.Abstractions;

// Write side Stock, commit oleh consumer/handler via IUnitOfWork.
public interface IStockRepository
{
    Task AddAsync(Stock stock, CancellationToken cancellationToken = default);

    Task<Stock?> GetAsync(StockId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsForReceiptLineAsync(Guid sourceGrId, int line, CancellationToken cancellationToken = default);
}
