using Wms.MasterData.Domain;

namespace Wms.MasterData.Application.Abstractions;

// Write side Product. Di commit oleh TransactionBehavior via IUnitOfWork.
public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task<Product?> GetAsync(Sku sku, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Sku sku, CancellationToken cancellationToken = default);
}
