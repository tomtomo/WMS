using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.ValueObjects;
using Wms.Inventory.Infrastructure;
using Wms.Inventory.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Inventory.IntegrationTests;

// klaim reservasi owned pada Stock persist inline
[Collection(PostgresCollection.Name)]
public sealed class ReservationPersistenceTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = InventoryTestHost.Build(connectionString);
        await InventoryTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Active_reservation_claim_persists_and_reduces_available_qty()
    {
        var stockId = await SeedAvailableStockAsync(qty: 100m);

        await MutateStockAsync(stockId, stock => stock.Reserve(
            StockReservationId.Create(Guid.NewGuid()).Value, Guid.NewGuid(), Guid.NewGuid(), Quantity.Create(30m).Value));

        var reloaded = await LoadStockAsync(stockId);
        reloaded.Qty.Should().Be(100m);
        reloaded.AvailableQty.Should().Be(70m, "klaim Active persist inline & mengurangi availableQty setelah reload");
    }

    [Fact]
    public async Task Concurrent_claim_on_same_stock_raises_xmin_conflict()
    {
        var stockId = await SeedAvailableStockAsync(qty: 100m);

        using var scopeA = _provider.CreateScope();
        using var scopeB = _provider.CreateScope();
        var contextA = scopeA.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var contextB = scopeB.ServiceProvider.GetRequiredService<InventoryDbContext>();

        var stockA = await contextA.Set<Stock>().FirstAsync(s => s.Id == StockId.Create(stockId).Value);
        var stockB = await contextB.Set<Stock>().FirstAsync(s => s.Id == StockId.Create(stockId).Value);

        stockA.Reserve(StockReservationId.Create(Guid.NewGuid()).Value, Guid.NewGuid(), Guid.NewGuid(), Quantity.Create(30m).Value);
        stockB.Reserve(StockReservationId.Create(Guid.NewGuid()).Value, Guid.NewGuid(), Guid.NewGuid(), Quantity.Create(30m).Value);

        await contextA.SaveChangesAsync();
        var act = async () => await contextB.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateConcurrencyException>(
            "menambah klaim membump xmin row Stock, commit kedua kalah, tidak boleh over-allocate");
    }

    private async Task<Guid> SeedAvailableStockAsync(decimal qty)
    {
        var stock = Stock.CreateOnHand(
            StockId.Create(Guid.NewGuid()).Value,
            Sku.Create("SKU-MILK").Value,
            LocationId.Create(Guid.NewGuid()).Value,
            Batch.Create("LOT-01").Value,
            Expiry.Create(new DateOnly(2026, 12, 31)).Value,
            Quantity.Create(qty).Value,
            Guid.NewGuid(),
            0,
            Guid.NewGuid()).Value;
        stock.PutAway(LocationId.Create(Guid.NewGuid()).Value);

        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        context.Add(stock);
        await context.SaveChangesAsync();
        return stock.Id.Value;
    }

    private async Task MutateStockAsync(Guid stockId, Func<Stock, Result> mutate)
    {
        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        var stock = await context.Set<Stock>().FirstAsync(s => s.Id == StockId.Create(stockId).Value);
        mutate(stock).IsSuccess.Should().BeTrue();
        await context.SaveChangesAsync();
    }

    private async Task<Stock> LoadStockAsync(Guid stockId)
    {
        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        return await context.Set<Stock>().AsNoTracking().FirstAsync(s => s.Id == StockId.Create(stockId).Value);
    }
}
