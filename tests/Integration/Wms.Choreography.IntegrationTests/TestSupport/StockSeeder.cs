using Microsoft.Extensions.DependencyInjection;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.ValueObjects;
using Wms.Inventory.Infrastructure;

namespace Wms.Choreography.IntegrationTests.TestSupport;

// Seed stok Available
internal static class StockSeeder
{
    public static async Task<Guid> SeedAvailableAsync(
        ServiceProvider provider,
        string sku,
        decimal qty,
        Guid warehouseId,
        string batch = "LOT-01",
        DateOnly? expiry = null)
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
            warehouseId).Value;

        stock.PutAway(LocationId.Create(Guid.NewGuid()).Value);

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        context.Add(stock);
        stock.ClearDomainEvents();
        await context.SaveChangesAsync();
        return stock.Id.Value;
    }
}
