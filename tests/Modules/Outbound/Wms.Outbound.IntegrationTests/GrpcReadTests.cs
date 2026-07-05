using AwesomeAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Wms.Outbound.Api.Grpc.V1;
using Wms.Outbound.Application.Features.CreateWave;
using Wms.Outbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Outbound.IntegrationTests;

// gRPC internal read Outbound.
[Collection(PostgresCollection.Name)]
public sealed class GrpcReadTests(PostgresFixture postgres) : IAsyncLifetime
{
    private WebApplication _app = null!;

    private GrpcChannel _channel = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _app = await ApiHostFactory.StartAsync(connectionString);
        await OutboundTestHost.MigrateAsync(_app.Services);

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
    public async Task GetWave_returns_summary_with_rollup()
    {
        var orderId = await OutboundSeeder.SeedNewOrderAsync(_app.Services, "SKU-MILK", 10m);
        var warehouseId = Guid.NewGuid();
        var waveId = (await PipelineRunner.SendAsync(_app.Services, new CreateWaveCommand([orderId], warehouseId))).Value;
        await PipelineRunner.ConsumeAsync(
            _app.Services,
            StockAllocationCompletedFactory.FullyAllocated(
                waveId, StockAllocationCompletedFactory.AllocationOf(orderId, "SKU-MILK", 10m, Guid.NewGuid())),
            Guid.NewGuid());

        var client = new OutboundReadService.OutboundReadServiceClient(_channel);
        var summary = await client.GetWaveAsync(new GetWaveRequest { WaveId = waveId.ToString() });

        summary.Status.Should().Be("Active");
        summary.WarehouseId.Should().Be(warehouseId.ToString());
        summary.PickingTaskCount.Should().Be(1);
        summary.CompletedPickingTaskCount.Should().Be(0);
    }

    [Fact]
    public async Task GetWave_unknown_is_not_found_with_trailer_error_code()
    {
        var client = new OutboundReadService.OutboundReadServiceClient(_channel);

        var call = async () => await client.GetWaveAsync(new GetWaveRequest { WaveId = Guid.NewGuid().ToString() });

        var exception = await call.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
        exception.Which.Trailers.GetValue("error-code").Should().Be("wave.not_found");
    }

    [Fact]
    public async Task GetWave_invalid_guid_is_invalid_argument()
    {
        var client = new OutboundReadService.OutboundReadServiceClient(_channel);

        var call = async () => await client.GetWaveAsync(new GetWaveRequest { WaveId = "bukan-guid" });

        (await call.Should().ThrowAsync<RpcException>()).Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }
}
