using AwesomeAssertions;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Wms.GrpcLookup.IntegrationTests.TestSupport;
using Wms.MasterData.Grpc.V1;
using Wms.MasterData.Infrastructure.Seed;
using Xunit;
using InboundGrpc = Wms.Inbound.Infrastructure.Grpc;
using OutboundGrpc = Wms.Outbound.Infrastructure.Grpc;

namespace Wms.GrpcLookup.IntegrationTests;

// Memastikan reader Inbound dan Outbound memvalidasi referensi lewat gRPC MasterData.
[Collection(GrpcLookupCollection.Name)]
public sealed class MasterDataGrpcLookupTests(GrpcLookupFixture fixture) : IAsyncLifetime
{
    private WebApplication _masterData = null!;
    private GrpcChannel _channel = null!;

    public async Task InitializeAsync()
    {
        var connectionString = await fixture.CreateFreshDatabaseAsync("masterdata");
        _masterData = await MasterDataLookupHost.StartAsync(connectionString);
        _channel = GrpcChannel.ForAddress(
            _masterData.GetTestClient().BaseAddress!,
            new GrpcChannelOptions { HttpHandler = _masterData.GetTestServer().CreateHandler() });
    }

    public async Task DisposeAsync()
    {
        _channel.Dispose();
        await _masterData.DisposeAsync();
    }

    [Fact]
    public async Task Inbound_product_reader_resolves_masterdata_via_real_grpc()
    {
        var reader = new InboundGrpc.ProductGrpcReader(new MasterDataLookup.MasterDataLookupClient(_channel));

        (await reader.ExistsAsync("SKU-MILK")).Should().BeTrue("SKU-MILK di-seed MasterData");
        (await reader.ExistsAsync("SKU-DOES-NOT-EXIST")).Should().BeFalse("NotFound gRPC → tidak ada");
    }

    [Fact]
    public async Task Inbound_warehouse_reader_resolves_masterdata_via_real_grpc()
    {
        var reader = new InboundGrpc.WarehouseGrpcReader(new MasterDataLookup.MasterDataLookupClient(_channel));

        (await reader.ExistsAsync(MasterDataSeeder.DefaultWarehouseId)).Should().BeTrue();
        (await reader.ExistsAsync(Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public async Task Outbound_warehouse_reader_resolves_masterdata_via_real_grpc()
    {
        var reader = new OutboundGrpc.WarehouseGrpcReader(new MasterDataLookup.MasterDataLookupClient(_channel));

        (await reader.ExistsAsync(MasterDataSeeder.DefaultWarehouseId)).Should().BeTrue();
        (await reader.ExistsAsync(Guid.NewGuid())).Should().BeFalse();
    }
}
