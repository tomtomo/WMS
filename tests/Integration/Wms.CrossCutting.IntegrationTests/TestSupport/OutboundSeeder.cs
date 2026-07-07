using Microsoft.Extensions.DependencyInjection;
using Wms.Outbound.Domain;
using Wms.Outbound.Domain.ValueObjects;
using Wms.Outbound.Infrastructure;

namespace Wms.CrossCutting.IntegrationTests.TestSupport;

// Seed OutboundOrder status New langsung ke DB Outbound.
internal static class OutboundSeeder
{
    public static async Task<Guid> SeedNewOrderAsync(ServiceProvider provider, string sku, decimal qty)
    {
        var carton = Uom.Create("CARTON").Value;
        var order = OutboundOrder.Create(
            OutboundOrderId.Create(Guid.NewGuid()).Value,
            Guid.NewGuid(),
            ShipTo.Create("Toko Tom", "Jl. Merdeka 1", "Jakarta").Value,
            [OrderLine.Create(sku, qty, carton).Value]).Value;
        order.ClearDomainEvents();

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OutboundDbContext>();
        context.Add(order);
        await context.SaveChangesAsync();
        return order.Id.Value;
    }
}
