using Microsoft.Extensions.DependencyInjection;
using Wms.Outbound.Domain;
using Wms.Outbound.Domain.ValueObjects;
using Wms.Outbound.Infrastructure;

namespace Wms.Outbound.IntegrationTests.TestSupport;

// Seed OutboundOrder backlog (New) langsung ke store — order diasumsikan datang dari sistem eksternal.
internal static class OutboundSeeder
{
    public static Task<Guid> SeedNewOrderAsync(
        IServiceProvider provider,
        string sku = "SKU-MILK",
        decimal qty = 10m) =>
        SeedMultiLineOrderAsync(provider, (sku, qty));

    // Order multi-line (multi-SKU) untuk skenario alokasi parsial.
    public static async Task<Guid> SeedMultiLineOrderAsync(
        IServiceProvider provider,
        params (string Sku, decimal Qty)[] lines)
    {
        var carton = Uom.Create("CARTON").Value;
        var order = OutboundOrder.Create(
            OutboundOrderId.Create(Guid.NewGuid()).Value,
            Guid.NewGuid(),
            ShipTo.Create("Toko Tom", "Jl. Merdeka 1", "Jakarta").Value,
            [.. lines.Select(line => OrderLine.Create(line.Sku, line.Qty, carton).Value)]).Value;
        order.ClearDomainEvents();

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OutboundDbContext>();
        context.Add(order);
        await context.SaveChangesAsync();
        return order.Id.Value;
    }
}
