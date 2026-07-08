using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Wms.Authorization.IntegrationTests.TestSupport;
using Wms.BuildingBlocks.Application.Abstractions.Ports;
using Wms.BuildingBlocks.Infrastructure.Persistence;
using Wms.Inbound.Domain;
using Wms.Inbound.Domain.ValueObjects;
using Wms.Inbound.Infrastructure;
using Xunit;

namespace Wms.Authorization.IntegrationTests;

// Memastikan user hanya melihat warehouse yang boleh diaksesnya.
[Collection(PostgresCollection.Name)]
public sealed class WarehouseScopingTests(PostgresFixture postgres) : IAsyncLifetime
{
    private readonly Guid _warehouse1 = Guid.NewGuid();
    private readonly Guid _warehouse2 = Guid.NewGuid();
    private string _connectionString = null!;

    public async Task InitializeAsync()
    {
        _connectionString = await postgres.CreateFreshDatabaseAsync();

        // Seed lewat konteks SYSTEM (bypass): 1 GR di W1, 1 di W2.
        await using var seed = NewContext(ScopedCurrentUser.System());
        await seed.Database.MigrateAsync();
        await SeedGoodsReceiptAsync(seed, _warehouse1);
        await SeedGoodsReceiptAsync(seed, _warehouse2);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_warehouse_scoped_user_sees_only_rows_of_their_warehouse()
    {
        await using var scoped = NewContext(ScopedCurrentUser.ScopedTo(_warehouse1));

        var visible = await scoped.Set<GoodsReceipt>().ToListAsync();

        visible.Should().ContainSingle("global query filter menyembunyikan warehouse lain");
        visible[0].WarehouseId.Should().Be(_warehouse1);
    }

    [Fact]
    public async Task A_user_with_no_assigned_warehouse_sees_nothing()
    {
        await using var scoped = NewContext(ScopedCurrentUser.ScopedTo());

        var visible = await scoped.Set<GoodsReceipt>().ToListAsync();

        visible.Should().BeEmpty("fail-closed: authenticated tanpa warehouse tak lihat apa pun");
    }

    [Fact]
    public async Task The_system_actor_bypasses_scope_and_sees_all_warehouses()
    {
        await using var system = NewContext(ScopedCurrentUser.System());

        var visible = await system.Set<GoodsReceipt>().ToListAsync();

        visible.Should().HaveCount(2, "SYSTEM/consumer bypass = cross warehouse");
    }

    [Fact]
    public async Task Management_path_ignore_query_filters_returns_cross_warehouse_rows()
    {
        await using var scoped = NewContext(ScopedCurrentUser.ScopedTo(_warehouse1));

        // IncludeAllWarehouses = IgnoreQueryFilters di reader management path.
        var all = await scoped.Set<GoodsReceipt>().IgnoreQueryFilters().ToListAsync();

        all.Should().HaveCount(2, "admin/management path lihat lintas-warehouse via IgnoreQueryFilters");
    }

    private static async Task SeedGoodsReceiptAsync(InboundDbContext context, Guid warehouseId)
    {
        var goodsReceipt = GoodsReceipt.Create(
            GoodsReceiptId.Create(Guid.NewGuid()).Value,
            "PO-" + Guid.NewGuid().ToString("N")[..6],
            Guid.NewGuid(),
            warehouseId,
            DockDoor.Create("D1").Value,
            [ExpectedLine.Create("SKU-1", 10m, "EA").Value]).Value;

        context.Add(goodsReceipt);
        await context.SaveChangesAsync();
    }

    private InboundDbContext NewContext(ICurrentUser currentUser) =>
        new(
            new DbContextOptionsBuilder<InboundDbContext>()
                .UseNpgsql(_connectionString)
                .UseSnakeCaseNamingConvention()
                .AddInterceptors(new AuditableInterceptor(currentUser, TimeProvider.System))
                .Options,
            currentUser);
}
