using Microsoft.EntityFrameworkCore;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.Enums;
using Wms.Inventory.Domain.ValueObjects;

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

    // Kandidat alokasi FEFO: Available per SKU, urut expiry terdekat
    public async Task<IReadOnlyList<Stock>> GetAllocatableAsync(Sku sku, CancellationToken cancellationToken = default) =>
        await context.Set<Stock>()
            .Where(stock => stock.Sku == sku && stock.Status == StockStatus.Available)
            .OrderBy(stock => stock.Expiry)
            .ThenBy(stock => stock.Batch)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Stock>> GetPickedByWaveAsync(Guid waveId, CancellationToken cancellationToken = default) =>
        await context.Set<Stock>()
            .Where(stock => stock.Status == StockStatus.Picked && stock.WaveId == waveId)
            .ToListAsync(cancellationToken);

    public void Remove(Stock stock) => context.Set<Stock>().Remove(stock);
}
