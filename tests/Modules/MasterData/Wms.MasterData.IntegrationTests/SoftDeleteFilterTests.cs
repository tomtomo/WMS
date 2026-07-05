using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Domain;
using Wms.MasterData.Infrastructure;
using Wms.MasterData.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.MasterData.IntegrationTests;

[Collection(PostgresCollection.Name)]
public sealed class SoftDeleteFilterTests(PostgresFixture postgres) : IAsyncLifetime
{
    private ServiceProvider _provider = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _provider = MasterDataTestHost.Build(connectionString);
        await MasterDataTestHost.MigrateAsync(_provider);
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task Deactivated_row_is_hidden_from_default_query()
    {
        var activeId = await MasterDataScenarios.CreateWarehouseAsync(_provider, "DC Active", "Addr 1");
        var inactiveId = await MasterDataScenarios.CreateWarehouseAsync(_provider, "DC Inactive", "Addr 2");
        await MasterDataScenarios.DeactivateWarehouseAsync(_provider, inactiveId);

        using var scope = _provider.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWarehouseReader>();

        (await reader.GetByIdAsync(inactiveId)).Should().BeNull("soft-deleted tersembunyi dari query default");
        (await reader.GetByIdAsync(activeId)).Should().NotBeNull();

        var activeOnly = await reader.ListAsync(1, 50, includeInactive: false);
        activeOnly.Items.Should().ContainSingle().Which.WarehouseId.Should().Be(activeId);
    }

    [Fact]
    public async Task IncludeInactive_bypass_returns_the_soft_deleted_row()
    {
        var activeId = await MasterDataScenarios.CreateWarehouseAsync(_provider, "DC Active", "Addr 1");
        var inactiveId = await MasterDataScenarios.CreateWarehouseAsync(_provider, "DC Inactive", "Addr 2");
        await MasterDataScenarios.DeactivateWarehouseAsync(_provider, inactiveId);

        using var scope = _provider.CreateScope();
        var reader = scope.ServiceProvider.GetRequiredService<IWarehouseReader>();

        var all = await reader.ListAsync(1, 50, includeInactive: true);
        all.Items.Select(warehouse => warehouse.WarehouseId).Should().Contain([activeId, inactiveId]);
    }

    [Fact]
    public async Task Deactivate_is_soft_delete_the_row_still_persists()
    {
        var id = await MasterDataScenarios.CreateWarehouseAsync(_provider, "DC X", "Addr");
        await MasterDataScenarios.DeactivateWarehouseAsync(_provider, id);

        using var scope = _provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MasterDataDbContext>();
        var warehouseId = WarehouseId.Create(id).Value;

        var rowStillExists = await context.Set<Warehouse>().IgnoreQueryFilters()
            .AnyAsync(warehouse => warehouse.Id == warehouseId);
        rowStillExists.Should().BeTrue("Deactivate = soft-delete, bukan hard delete (referential integrity dokumen historis)");
    }
}
