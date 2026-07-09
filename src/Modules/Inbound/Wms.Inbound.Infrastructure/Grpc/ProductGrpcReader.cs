using Grpc.Core;
using Wms.Inbound.Application.Abstractions;
using Wms.MasterData.Grpc.V1;

namespace Wms.Inbound.Infrastructure.Grpc;

// Reader SKU lewat gRPC ke MasterData, dipakai untuk validasi produk antar service.
public sealed class ProductGrpcReader(MasterDataLookup.MasterDataLookupClient client) : IProductReader
{
    public async Task<bool> ExistsAsync(string sku, CancellationToken cancellationToken = default)
    {
        try
        {
            await client.GetProductBySkuAsync(new GetProductBySkuRequest { Sku = sku }, cancellationToken: cancellationToken);
            return true;
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return false;
        }
    }
}
