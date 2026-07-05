using Microsoft.Extensions.DependencyInjection;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.ValueObjects;
using Wms.Inventory.Infrastructure;

namespace Wms.Inventory.IntegrationTests.TestSupport;

// Seed balance langsung ke store untuk test alokasi/picking/dispatch/expiry.
internal static class StockSeeder
{
    // Balance Available (allocatable). Kembalikan stockId.
    public static Task<Guid> SeedAvailableAsync(
        IServiceProvider provider,
        string sku = "SKU-MILK",
        decimal qty = 100m,
        string batch = "LOT-01",
        DateOnly? expiry = null,
        Guid? warehouseId = null)
    {
        var stock = Stock.CreateOnHand(
            StockId.Create(Guid.NewGuid()).Value,
            Sku.Create(sku).Value,
            LocationId.Create(Guid.NewGuid()).Value,
            Batch.Create(batch).Value,
            Expiry.Create(expiry ?? new DateOnly(2026, 12, 31)).Value,
            Quantity.Create(qty).Value,
            Guid.NewGuid(),
            0,
            warehouseId ?? Guid.NewGuid()).Value;
        stock.PutAway(LocationId.Create(Guid.NewGuid()).Value);
        return SaveAsync(provider, stock);
    }

    // Balance OnHand (belum putaway, tak allocatable) — dipakai test expiry scan yang memindai OnHand juga.
    public static Task<Guid> SeedOnHandAsync(
        IServiceProvider provider,
        string sku = "SKU-MILK",
        decimal qty = 100m,
        string batch = "LOT-01",
        DateOnly? expiry = null,
        Guid? warehouseId = null)
    {
        var stock = Stock.CreateOnHand(
            StockId.Create(Guid.NewGuid()).Value,
            Sku.Create(sku).Value,
            LocationId.Create(Guid.NewGuid()).Value,
            Batch.Create(batch).Value,
            Expiry.Create(expiry ?? new DateOnly(2026, 12, 31)).Value,
            Quantity.Create(qty).Value,
            Guid.NewGuid(),
            0,
            warehouseId ?? Guid.NewGuid()).Value;
        return SaveAsync(provider, stock);
    }

    private static async Task<Guid> SaveAsync(IServiceProvider provider, Stock stock)
    {
        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        context.Add(stock);
        stock.ClearDomainEvents();
        await context.SaveChangesAsync();
        return stock.Id.Value;
    }
}
