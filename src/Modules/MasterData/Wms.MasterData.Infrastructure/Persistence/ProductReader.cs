using Microsoft.EntityFrameworkCore;
using Wms.BuildingBlocks.Application.ReadModels;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Application.ReadModels;
using Wms.MasterData.Domain;

namespace Wms.MasterData.Infrastructure.Persistence;

// Read port EF Product — AsNoTracking
internal sealed class ProductReader(MasterDataDbContext context) : IProductReader
{
    public async Task<ProductSnapshotDto?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var parsed = Sku.Create(sku);
        if (parsed.IsFailure)
        {
            return null;
        }

        var product = await context.Set<Product>().AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == parsed.Value, cancellationToken);
        return product is null ? null : Map(product);
    }

    public async Task<PagedResult<ProductSnapshotDto>> ListAsync(
        int page,
        int pageSize,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var currentPage = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 200);

        IQueryable<Product> query = context.Set<Product>().AsNoTracking();
        if (includeInactive)
        {
            query = query.IgnoreQueryFilters();
        }

        var total = await query.CountAsync(cancellationToken);
        var products = await query
            .OrderBy(product => product.Id)
            .Skip((currentPage - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductSnapshotDto>([.. products.Select(Map)], total, currentPage, size);
    }

    private static ProductSnapshotDto Map(Product product) => new(
        product.Sku.Value,
        product.Name,
        product.Uom,
        product.BatchTrackingRequired,
        product.ExpiryTrackingRequired,
        product.QcRequiredOnReceipt,
        product.ShelfLifeDays,
        product.IsActive);
}
