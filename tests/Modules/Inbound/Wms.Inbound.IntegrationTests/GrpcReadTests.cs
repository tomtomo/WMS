using AwesomeAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Wms.Inbound.Api.Grpc.V1;
using Wms.Inbound.IntegrationTests.TestSupport;
using Xunit;

namespace Wms.Inbound.IntegrationTests;

// gRPC internal read
[Collection(PostgresCollection.Name)]
public sealed class GrpcReadTests(PostgresFixture postgres) : IAsyncLifetime
{
    private WebApplication _app = null!;

    private GrpcChannel _channel = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await postgres.CreateFreshDatabaseAsync();
        _app = await ApiHostFactory.StartAsync(connectionString);
        await InboundTestHost.MigrateAsync(_app.Services);

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
    public async Task GetGoodsReceipt_mengembalikan_summary()
    {
        var grId = await GoodsReceiptScenarios.CreateAsync(_app.Services, ("SKU-A", 10m));
        await GoodsReceiptScenarios.ScanAsync(_app.Services, grId, "SKU-A", 12m);
        await GoodsReceiptScenarios.CompleteScanAsync(_app.Services, grId);

        var client = new GoodsReceiptReadService.GoodsReceiptReadServiceClient(_channel);
        var summary = await client.GetGoodsReceiptAsync(new GetGoodsReceiptRequest
        {
            GoodsReceiptId = grId.ToString(),
        });

        summary.PoRef.Should().Be("PO-2026-001");
        summary.Status.Should().Be("Pending");
        summary.SupplierId.Should().Be(GoodsReceiptScenarios.SupplierId.ToString());
        summary.DiscrepancyCount.Should().Be(1);
        summary.UnresolvedDiscrepancyCount.Should().Be(1);
    }

    [Fact]
    public async Task GetGoodsReceipt_tidak_ada_notfound_dengan_trailer_error_code()
    {
        var client = new GoodsReceiptReadService.GoodsReceiptReadServiceClient(_channel);

        var call = async () => await client.GetGoodsReceiptAsync(new GetGoodsReceiptRequest
        {
            GoodsReceiptId = Guid.NewGuid().ToString(),
        });

        var exception = await call.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
        exception.Which.Trailers.GetValue("error-code").Should().Be("goods_receipt.not_found");
    }

    [Fact]
    public async Task GetGoodsReceipt_guid_rusak_invalid_argument()
    {
        var client = new GoodsReceiptReadService.GoodsReceiptReadServiceClient(_channel);

        var call = async () => await client.GetGoodsReceiptAsync(new GetGoodsReceiptRequest
        {
            GoodsReceiptId = "bukan-guid",
        });

        var exception = await call.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }
}
