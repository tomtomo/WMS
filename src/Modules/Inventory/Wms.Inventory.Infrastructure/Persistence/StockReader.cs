using Microsoft.EntityFrameworkCore;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Application.ReadModels;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Domain.ValueObjects;

namespace Wms.Inventory.Infrastructure.Persistence;

// Read port balance — AsNoTracking, map ke DTO tanpa AutoMapper.
internal sealed class StockReader(InventoryDbContext context) : IStockReader
{
    public async Task<IReadOnlyList<AvailableStockView>> GetAvailableAsync(
        Guid warehouseId,
        string? sku,
        CancellationToken cancellationToken = default)
    {
        var query = context.Set<Stock>().AsNoTracking()
            .Where(stock => stock.WarehouseId == warehouseId && stock.Status == StockStatus.Available);

        if (!string.IsNullOrWhiteSpace(sku))
        {
            var skuFilter = Sku.Create(sku);
            if (skuFilter.IsFailure)
            {
                return [];
            }

            query = query.Where(stock => stock.Sku == skuFilter.Value);
        }

        var balances = await query.ToListAsync(cancellationToken);
        return [.. balances.Select(Map)];
    }

    public async Task<AvailableStockView?> GetByIdAsync(Guid stockId, CancellationToken cancellationToken = default)
    {
        var id = StockId.Create(stockId);
        if (id.IsFailure)
        {
            return null;
        }

        var stock = await context.Set<Stock>().AsNoTracking()
            .FirstOrDefaultAsync(balance => balance.Id == id.Value, cancellationToken);
        return stock is null ? null : Map(stock);
    }

    public async Task<IReadOnlyList<AvailableStockView>> GetExpiringAsync(
        DateOnly threshold,
        CancellationToken cancellationToken = default)
    {
        // Filter expiry di memori
        var active = await context.Set<Stock>().AsNoTracking()
            .Where(stock => stock.Status == StockStatus.Available || stock.Status == StockStatus.OnHand)
            .ToListAsync(cancellationToken);
        return [.. active.Where(stock => stock.Expiry.Value <= threshold).Select(Map)];
    }

    // availableQty dari domain
    private static AvailableStockView Map(Stock stock) => new(
        stock.Id.Value,
        stock.Sku.Value,
        stock.WarehouseId,
        stock.LocationId.Value,
        stock.Batch.Value,
        stock.Expiry.Value,
        stock.Qty,
        stock.AvailableQty,
        stock.Status.ToString());
}
