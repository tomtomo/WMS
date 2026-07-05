using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.MasterData.IntegrationTests;

// Read port CQRS
[Collection(PostgresCollection.Name)]
public sealed class ReadPortTests(PostgresFixture postgres) : IAsyncLifetime
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
    public async Task Warehouse_reader_returns_the_projected_dto()
    {
        var id = await MasterDataScenarios.CreateWarehouseAsync(_provider, "DC Jakarta Cakung", "Jl. Raya Cakung No. 1");

        using var scope = _provider.CreateScope();
        var warehouse = await scope.ServiceProvider.GetRequiredService<IWarehouseReader>().GetByIdAsync(id);

        warehouse.Should().NotBeNull();
        warehouse!.Name.Should().Be("DC Jakarta Cakung");
        warehouse.Address.Should().Be("Jl. Raya Cakung No. 1");
        warehouse.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Location_reader_flattens_type_to_string()
    {
        var warehouseId = await MasterDataScenarios.CreateWarehouseAsync(_provider);
        var locationId = await MasterDataScenarios.CreateLocationAsync(_provider, warehouseId, "StagingArea", "STG-2");

        using var scope = _provider.CreateScope();
        var location = await scope.ServiceProvider.GetRequiredService<ILocationReader>().GetByIdAsync(locationId);

        location.Should().NotBeNull();
        location!.WarehouseId.Should().Be(warehouseId);
        location.Type.Should().Be("StagingArea");
        location.Code.Should().Be("STG-2");
    }

    [Fact]
    public async Task Product_snapshot_is_available_and_filled_for_downstream_snapshotting()
    {
        await MasterDataScenarios.CreateProductAsync(
            _provider, "SKU-YOGURT", "Yogurt 500g", "carton", batchTracking: true, expiryTracking: true, qcRequired: true, shelfLifeDays: 21);

        using var scope = _provider.CreateScope();
        var snapshot = await scope.ServiceProvider.GetRequiredService<IProductReader>().GetBySkuAsync("SKU-YOGURT");

        snapshot.Should().NotBeNull();
        snapshot!.Sku.Should().Be("SKU-YOGURT");
        snapshot.Name.Should().Be("Yogurt 500g");
        snapshot.Uom.Should().Be("carton");
        snapshot.BatchTrackingRequired.Should().BeTrue();
        snapshot.ExpiryTrackingRequired.Should().BeTrue();
        snapshot.QcRequiredOnReceipt.Should().BeTrue();
        snapshot.ShelfLifeDays.Should().Be(21);
        snapshot.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Product_without_shelf_life_snapshots_a_null_shelf_life()
    {
        await MasterDataScenarios.CreateProductAsync(
            _provider, "SKU-RICE", "Rice 5kg", "sack", batchTracking: false, expiryTracking: false, qcRequired: false, shelfLifeDays: null);

        using var scope = _provider.CreateScope();
        var snapshot = await scope.ServiceProvider.GetRequiredService<IProductReader>().GetBySkuAsync("SKU-RICE");

        snapshot.Should().NotBeNull();
        snapshot!.ShelfLifeDays.Should().BeNull();
    }

    [Fact]
    public async Task Product_list_is_paged()
    {
        for (var i = 0; i < 3; i++)
        {
            await MasterDataScenarios.CreateProductAsync(_provider, $"SKU-{i:D3}", $"Product {i}", "piece", false, false, false, null);
        }

        using var scope = _provider.CreateScope();
        var firstPage = await scope.ServiceProvider.GetRequiredService<IProductReader>().ListAsync(page: 1, pageSize: 2);

        firstPage.TotalCount.Should().Be(3);
        firstPage.Items.Should().HaveCount(2);
        firstPage.Page.Should().Be(1);
        firstPage.PageSize.Should().Be(2);
    }
}
