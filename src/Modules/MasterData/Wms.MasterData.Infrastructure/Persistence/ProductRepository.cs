using Microsoft.EntityFrameworkCore;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Domain;

namespace Wms.MasterData.Infrastructure.Persistence;

// Write side Product. Di commit oleh TransactionBehavior via IUnitOfWork.
internal sealed class ProductRepository(MasterDataDbContext context) : IProductRepository
{
    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        context.Set<Product>().Add(product);
        return Task.CompletedTask;
    }

    // termasuk yang soft deleted, IgnoreQueryFilters.
    public Task<Product?> GetAsync(Sku sku, CancellationToken cancellationToken = default) =>
        context.Set<Product>().IgnoreQueryFilters()
            .FirstOrDefaultAsync(product => product.Id == sku, cancellationToken);

    public Task<bool> ExistsAsync(Sku sku, CancellationToken cancellationToken = default) =>
        context.Set<Product>().IgnoreQueryFilters()
            .AnyAsync(product => product.Id == sku, cancellationToken);
}
