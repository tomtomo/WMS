using Grpc.Core;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Web.GrpcInterceptors;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Grpc.V1;

namespace Wms.MasterData.Api.GrpcServices;

// Read only gRPC internal
public sealed class MasterDataLookupService(
    IWarehouseReader warehouseReader,
    ILocationReader locationReader,
    IProductReader productReader)
    : MasterDataLookup.MasterDataLookupBase
{
    public override async Task<WarehouseSnapshot> GetWarehouseById(GetWarehouseByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (!Guid.TryParse(request.WarehouseId, out var warehouseId))
        {
            throw new ResultFailureException(
                ResultErrorType.Validation, new Error("warehouse.id_invalid", "warehouseId bukan GUID valid."));
        }

        var warehouse = await warehouseReader.GetByIdAsync(warehouseId, context.CancellationToken);
        if (warehouse is null)
        {
            throw new ResultFailureException(
                ResultErrorType.NotFound, new Error("warehouse.not_found", "Warehouse tidak ditemukan."));
        }

        return new WarehouseSnapshot
        {
            WarehouseId = warehouse.WarehouseId.ToString(),
            Name = warehouse.Name,
            Address = warehouse.Address,
        };
    }

    public override async Task<LocationSnapshot> GetLocationById(GetLocationByIdRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (!Guid.TryParse(request.LocationId, out var locationId))
        {
            throw new ResultFailureException(
                ResultErrorType.Validation, new Error("location.id_invalid", "locationId bukan GUID valid."));
        }

        var location = await locationReader.GetByIdAsync(locationId, context.CancellationToken);
        if (location is null)
        {
            throw new ResultFailureException(
                ResultErrorType.NotFound, new Error("location.not_found", "Location tidak ditemukan."));
        }

        return new LocationSnapshot
        {
            LocationId = location.LocationId.ToString(),
            WarehouseId = location.WarehouseId.ToString(),
            Type = location.Type,
            Code = location.Code,
        };
    }

    public override async Task<ProductSnapshot> GetProductBySku(GetProductBySkuRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var product = await productReader.GetBySkuAsync(request.Sku, context.CancellationToken);
        if (product is null)
        {
            throw new ResultFailureException(
                ResultErrorType.NotFound, new Error("product.not_found", "Product tidak ditemukan."));
        }

        var snapshot = new ProductSnapshot
        {
            Sku = product.Sku,
            Name = product.Name,
            Uom = product.Uom,
            BatchTrackingRequired = product.BatchTrackingRequired,
            ExpiryTrackingRequired = product.ExpiryTrackingRequired,
            QcRequiredOnReceipt = product.QcRequiredOnReceipt,
        };

        if (product.ShelfLifeDays.HasValue)
        {
            snapshot.ShelfLifeDays = product.ShelfLifeDays.Value;
        }

        return snapshot;
    }
}
