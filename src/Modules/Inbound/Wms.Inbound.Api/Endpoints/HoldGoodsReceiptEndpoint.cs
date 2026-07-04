using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Inbound.Application.Features.HoldGoodsReceipt;

namespace Wms.Inbound.Api.Endpoints;

// POST /v1/goods-receipts/{id}/hold
public sealed class HoldGoodsReceiptEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        InboundApiRoutes.GoodsReceipts(app)
            .MapPost("/{goodsReceiptId:guid}/hold", HandleAsync)
            .WithName("HoldGoodsReceipt")
            .WithIdempotencyKey();

    private static async Task<IResult> HandleAsync(
        Guid goodsReceiptId,
        HoldGoodsReceiptRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new HoldGoodsReceiptCommand(goodsReceiptId, request.Reason), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }
}

public sealed record HoldGoodsReceiptRequest(string Reason);
