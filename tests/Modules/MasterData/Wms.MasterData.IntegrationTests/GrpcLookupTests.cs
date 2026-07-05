using AwesomeAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Wms.MasterData.Grpc.V1;
using Wms.MasterData.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.MasterData.IntegrationTests;

// gRPC read API masterdata.v1
[Collection(PostgresCollection.Name)]
public sealed class GrpcLookupTests(PostgresFixture postgres) : IAsyncLifetime
{
    private WebApplication _app = null!;
    private GrpcChannel _channel = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _app = await ApiHostFactory.StartAsync(connectionString);
        await MasterDataTestHost.MigrateAsync(_app.Services);

        _channel = GrpcChannel.ForAddress(
            _app.GetTestClient().BaseAddress!,
            new GrpcChannelOptions { HttpHandler = _app.GetTestServer().CreateHandler() });
    }

    public async Task DisposeAsync()
    {
        _channel.Dispose();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task GetWarehouseById_returns_snapshot()
    {
        var id = await MasterDataScenarios.CreateWarehouseAsync(_app.Services, "DC Jakarta Cakung", "Jl. Raya Cakung No. 1");
        var client = new MasterDataLookup.MasterDataLookupClient(_channel);

        var snapshot = await client.GetWarehouseByIdAsync(new GetWarehouseByIdRequest { WarehouseId = id.ToString() });

        snapshot.WarehouseId.Should().Be(id.ToString());
        snapshot.Name.Should().Be("DC Jakarta Cakung");
        snapshot.Address.Should().Be("Jl. Raya Cakung No. 1");
    }

    [Fact]
    public async Task GetLocationById_returns_snapshot()
    {
        var warehouseId = await MasterDataScenarios.CreateWarehouseAsync(_app.Services);
        var locationId = await MasterDataScenarios.CreateLocationAsync(_app.Services, warehouseId, "QuarantineArea", "QC-A");
        var client = new MasterDataLookup.MasterDataLookupClient(_channel);

        var snapshot = await client.GetLocationByIdAsync(new GetLocationByIdRequest { LocationId = locationId.ToString() });

        snapshot.WarehouseId.Should().Be(warehouseId.ToString());
        snapshot.Type.Should().Be("QuarantineArea");
        snapshot.Code.Should().Be("QC-A");
    }

    [Fact]
    public async Task GetProductBySku_returns_snapshot_with_tracking_flags()
    {
        await MasterDataScenarios.CreateProductAsync(
            _app.Services, "SKU-MILK", "Fresh Milk 1L", "carton", batchTracking: true, expiryTracking: true, qcRequired: false, shelfLifeDays: 30);
        var client = new MasterDataLookup.MasterDataLookupClient(_channel);

        var snapshot = await client.GetProductBySkuAsync(new GetProductBySkuRequest { Sku = "SKU-MILK" });

        snapshot.Sku.Should().Be("SKU-MILK");
        snapshot.Uom.Should().Be("carton");
        snapshot.BatchTrackingRequired.Should().BeTrue();
        snapshot.ExpiryTrackingRequired.Should().BeTrue();
        snapshot.HasShelfLifeDays.Should().BeTrue();
        snapshot.ShelfLifeDays.Should().Be(30);
    }

    [Fact]
    public async Task GetWarehouseById_unknown_is_not_found_with_trailer_error_code()
    {
        var client = new MasterDataLookup.MasterDataLookupClient(_channel);

        var call = async () => await client.GetWarehouseByIdAsync(new GetWarehouseByIdRequest { WarehouseId = Guid.NewGuid().ToString() });

        var exception = await call.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
        exception.Which.Trailers.GetValue("error-code").Should().Be("warehouse.not_found");
    }

    [Fact]
    public async Task GetProductBySku_unknown_is_not_found()
    {
        var client = new MasterDataLookup.MasterDataLookupClient(_channel);

        var call = async () => await client.GetProductBySkuAsync(new GetProductBySkuRequest { Sku = "SKU-DOES-NOT-EXIST" });

        var exception = await call.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
        exception.Which.Trailers.GetValue("error-code").Should().Be("product.not_found");
    }

    [Fact]
    public async Task GetWarehouseById_invalid_guid_is_invalid_argument()
    {
        var client = new MasterDataLookup.MasterDataLookupClient(_channel);

        var call = async () => await client.GetWarehouseByIdAsync(new GetWarehouseByIdRequest { WarehouseId = "bukan-guid" });

        var exception = await call.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }
}
