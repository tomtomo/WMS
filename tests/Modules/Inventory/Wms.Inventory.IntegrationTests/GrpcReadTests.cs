using AwesomeAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Wms.Inventory.Api.Grpc.V1;
using Wms.Inventory.Application.Features.CompletePutaway;
using Wms.Inventory.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Inventory.IntegrationTests;

// gRPC internal read Inventory.
[Collection(PostgresCollection.Name)]
public sealed class GrpcReadTests(PostgresFixture postgres) : IAsyncLifetime
{
    private WebApplication _app = null!;

    private GrpcChannel _channel = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _app = await ApiHostFactory.StartAsync(connectionString);
        await InventoryTestHost.MigrateAsync(_app.Services);

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
    public async Task GetStock_returns_summary()
    {
        (await PipelineRunner.ConsumeAsync(
            _app.Services,
            GrConfirmedFactory.With(Guid.NewGuid(), GrConfirmedFactory.Good(qty: 100m)),
            Guid.NewGuid())).IsSuccess.Should().BeTrue();
        var task = (await PipelineRunner.TasksAsync(_app.Services)).Single();
        await PipelineRunner.SendAsync(_app.Services, new CompletePutawayCommand(task.Id.Value, Guid.NewGuid(), null));
        var stock = (await PipelineRunner.StocksAsync(_app.Services)).Single();

        var client = new InventoryReadService.InventoryReadServiceClient(_channel);
        var summary = await client.GetStockAsync(new GetStockRequest { StockId = stock.Id.Value.ToString() });

        summary.Sku.Should().Be("SKU-MILK");
        summary.Status.Should().Be("Available");
        summary.WarehouseId.Should().Be(GrConfirmedFactory.WarehouseId.ToString());
        summary.Qty.Should().Be(100d);
        summary.AvailableQty.Should().Be(100d);
    }

    [Fact]
    public async Task GetStock_unknown_is_not_found_with_trailer_error_code()
    {
        var client = new InventoryReadService.InventoryReadServiceClient(_channel);

        var call = async () => await client.GetStockAsync(new GetStockRequest { StockId = Guid.NewGuid().ToString() });

        var exception = await call.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
        exception.Which.Trailers.GetValue("error-code").Should().Be("stock.not_found");
    }

    [Fact]
    public async Task GetStock_invalid_guid_is_invalid_argument()
    {
        var client = new InventoryReadService.InventoryReadServiceClient(_channel);

        var call = async () => await client.GetStockAsync(new GetStockRequest { StockId = "bukan-guid" });

        var exception = await call.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }
}
