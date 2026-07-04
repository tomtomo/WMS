using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.Inventory.Domain;
using Wms.Inventory.Domain.ValueObjects;
using Wms.Inventory.Infrastructure;
using Wms.Inventory.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Inventory.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class StockPersistenceTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task Unique_index_rejects_two_receiving_balances_with_same_gr_line()
    {
        var grId = Guid.NewGuid();
        await AddAndSaveAsync(NewReceivingStock(grId, line: 0));

        var act = async () => await AddAndSaveAsync(NewReceivingStock(grId, line: 0));

        await act.Should().ThrowAsync<DbUpdateException>("natural key (sourceGrId, line) unik untuk balance receiving — anti stok hantu");
    }

    [Fact]
    public async Task Picked_split_child_coexists_with_parent_sharing_gr_line()
    {
        var grId = Guid.NewGuid();
        var (parent, picked) = ReceiveReserveAndPick(grId, line: 0);

        var act = async () =>
        {
            using var scope = _provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            context.Add(parent);
            context.Add(picked);
            await context.SaveChangesAsync();
        };

        await act.Should().NotThrowAsync(
            "balance Picked hasil split berbagi (grId, line) dgn parent — unik hanya berlaku untuk balance non-Picked");
    }

    private static Stock NewReceivingStock(Guid grId, int line) =>
        Stock.CreateOnHand(
            StockId.Create(Guid.NewGuid()).Value,
            Sku.Create("SKU-MILK").Value,
            LocationId.Create(Guid.NewGuid()).Value,
            Batch.Create("LOT-01").Value,
            Expiry.Create(new DateOnly(2026, 12, 31)).Value,
            Quantity.Create(100m).Value,
            grId,
            line,
            Guid.NewGuid()).Value;

    private static (Stock Parent, Stock Picked) ReceiveReserveAndPick(Guid grId, int line)
    {
        var parent = NewReceivingStock(grId, line);
        parent.PutAway(LocationId.Create(Guid.NewGuid()).Value);
        var reservationId = StockReservationId.Create(Guid.NewGuid()).Value;
        parent.Reserve(reservationId, Guid.NewGuid(), Guid.NewGuid(), Quantity.Create(40m).Value);
        var picked = parent.Pick(
            reservationId,
            StockId.Create(Guid.NewGuid()).Value,
            Guid.NewGuid(),
            LocationId.Create(Guid.NewGuid()).Value).Value;
        return (parent, picked);
    }

    private async Task AddAndSaveAsync(Stock stock)
    {
        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
        context.Add(stock);
        await context.SaveChangesAsync();
    }
}
