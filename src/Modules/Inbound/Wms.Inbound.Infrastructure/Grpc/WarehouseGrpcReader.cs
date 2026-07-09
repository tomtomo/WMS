using Grpc.Core;
using Wms.Inbound.Application.Abstractions;
using Wms.MasterData.Grpc.V1;

namespace Wms.Inbound.Infrastructure.Grpc;

// Reader warehouse lewat gRPC ke MasterData, dipakai untuk validasi warehouse antar service.
public sealed class WarehouseGrpcReader(MasterDataLookup.MasterDataLookupClient client) : IWarehouseReader
{
    public async Task<bool> ExistsAsync(Guid warehouseId, CancellationToken cancellationToken = default)
    {
        try
        {
            await client.GetWarehouseByIdAsync(
                new GetWarehouseByIdRequest { WarehouseId = warehouseId.ToString() }, cancellationToken: cancellationToken);
            return true;
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return false;
        }
    }
}
