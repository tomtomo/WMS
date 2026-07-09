using Microsoft.EntityFrameworkCore;
using Wms.Inventory.Application.Abstractions;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.Allocation;
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

    // Kandidat alokasi FEFO: Available per SKU, urutkan via FefoSelector domain
    public async Task<IReadOnlyList<Stock>> GetAllocatableAsync(Sku sku, CancellationToken cancellationToken = default)
    {
        var candidates = await context.Set<Stock>()
            .Where(stock => stock.Sku == sku && stock.Status == StockStatus.Available)
            .ToListAsync(cancellationToken);
        return FefoSelector.Order(candidates);
    }

    public async Task<IReadOnlyList<Stock>> GetPickedByWaveAsync(Guid waveId, CancellationToken cancellationToken = default) =>
        await context.Set<Stock>()
            .Where(stock => stock.Status == StockStatus.Picked && stock.WaveId == waveId)
            .ToListAsync(cancellationToken);

    public void Remove(Stock stock) => context.Set<Stock>().Remove(stock);
}
