using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Web;
using Wms.Inbound.Application.Abstractions;

namespace Wms.Inbound.Api.Endpoints;

// GET /v1/goods-receipts/{id} - read langsung ke read port (tanpa pipeline command).
public sealed class GetGoodsReceiptEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        InboundApiRoutes.GoodsReceipts(app)
            .MapGet("/{goodsReceiptId:guid}", HandleAsync)
            .WithName("GetGoodsReceipt");

    private static async Task<IResult> HandleAsync(
        Guid goodsReceiptId,
        IGoodsReceiptReader reader,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var detail = await reader.GetDetailAsync(goodsReceiptId, cancellationToken);
        return detail is null
            ? Result.NotFound(new Error("goods_receipt.not_found", "GoodsReceipt tidak ditemukan.")).ToProblem(httpContext)
            : Results.Ok(detail);
    }
}
