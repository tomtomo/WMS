using Grpc.Core;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Web.GrpcInterceptors;
using Wms.Inventory.Api.Grpc.V1;
using Wms.Inventory.Application.Abstractions;

namespace Wms.Inventory.Api.GrpcServices;

// Read only gRPC internal — Result.Failure dibawa ResultFailureException, ErrorMappingInterceptor memetakan ke status gRPC.
public sealed class InventoryReadGrpcService(IStockReader reader)
    : InventoryReadService.InventoryReadServiceBase
{
    public override async Task<StockSummary> GetStock(GetStockRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (!Guid.TryParse(request.StockId, out var stockId))
        {
            throw new ResultFailureException(
                ResultErrorType.Validation,
                new Error("stock.id_invalid", "stockId bukan GUID valid."));
        }

        var view = await reader.GetByIdAsync(stockId, context.CancellationToken);
        if (view is null)
        {
            throw new ResultFailureException(
                ResultErrorType.NotFound,
                new Error("stock.not_found", "Stock tidak ditemukan."));
        }

        return new StockSummary
        {
            StockId = view.StockId.ToString(),
            Sku = view.Sku,
            WarehouseId = view.WarehouseId.ToString(),
            LocationId = view.LocationId.ToString(),
            Batch = view.Batch,
            Status = view.Status,
            Qty = (double)view.Qty,
            AvailableQty = (double)view.AvailableQty,
        };
    }
}
