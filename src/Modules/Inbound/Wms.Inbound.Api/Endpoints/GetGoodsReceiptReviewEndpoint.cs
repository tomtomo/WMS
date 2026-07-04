using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Web;
using Wms.Inbound.Application.Abstractions;

namespace Wms.Inbound.Api.Endpoints;

// GET /v1/goods-receipts/{id}/review
public sealed class GetGoodsReceiptReviewEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        InboundApiRoutes.GoodsReceipts(app)
            .MapGet("/{goodsReceiptId:guid}/review", HandleAsync)
            .WithName("GetGoodsReceiptReview");

    private static async Task<IResult> HandleAsync(
        Guid goodsReceiptId,
        IGoodsReceiptReader reader,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var review = await reader.GetReviewAsync(goodsReceiptId, cancellationToken);
        return review is null
            ? Result.NotFound(new Error("goods_receipt.not_found", "GoodsReceipt tidak ditemukan.")).ToProblem(httpContext)
            : Results.Ok(review);
    }
}
