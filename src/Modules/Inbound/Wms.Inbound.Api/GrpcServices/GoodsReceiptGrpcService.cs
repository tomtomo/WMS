using Grpc.Core;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Web.GrpcInterceptors;
using Wms.Inbound.Api.Grpc.V1;
using Wms.Inbound.Application.Abstractions;

namespace Wms.Inbound.Api.GrpcServices;

// Read-only gRPC internal - Result.Failure dibawa ResultFailureException, ErrorMappingInterceptor yang memetakan ke status gRPC
public sealed class GoodsReceiptGrpcService(IGoodsReceiptReader reader)
    : GoodsReceiptReadService.GoodsReceiptReadServiceBase
{
    public override async Task<GoodsReceiptSummary> GetGoodsReceipt(
        GetGoodsReceiptRequest request,
        ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (!Guid.TryParse(request.GoodsReceiptId, out var goodsReceiptId))
        {
            throw new ResultFailureException(
                ResultErrorType.Validation,
                new Error("goods_receipt.id_invalid", "goodsReceiptId bukan GUID valid."));
        }

        var detail = await reader.GetDetailAsync(goodsReceiptId, context.CancellationToken);
        if (detail is null)
        {
            throw new ResultFailureException(
                ResultErrorType.NotFound,
                new Error("goods_receipt.not_found", "GoodsReceipt tidak ditemukan."));
        }

        var resolvedIds = detail.Resolutions.Select(resolution => resolution.DiscrepancyId).ToHashSet();
        return new GoodsReceiptSummary
        {
            GoodsReceiptId = detail.GoodsReceiptId.ToString(),
            PoRef = detail.PoRef,
            SupplierId = detail.SupplierId.ToString(),
            WarehouseId = detail.WarehouseId.ToString(),
            DockDoor = detail.DockDoor,
            Status = detail.Status,
            DiscrepancyCount = detail.Discrepancies.Count,
            UnresolvedDiscrepancyCount = detail.Discrepancies.Count(d => !resolvedIds.Contains(d.DiscrepancyId)),
        };
    }
}
