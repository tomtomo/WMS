using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Inbound.Application.Features.CompleteScan;

namespace Wms.Inbound.Api.Endpoints;

// POST /v1/goods-receipts/{id}/complete-scan.
public sealed class CompleteScanEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        InboundApiRoutes.GoodsReceipts(app)
            .MapPost("/{goodsReceiptId:guid}/complete-scan", HandleAsync)
            .WithName("CompleteScan")
            .WithIdempotencyKey();

    private static async Task<IResult> HandleAsync(
        Guid goodsReceiptId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CompleteScanCommand(goodsReceiptId), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }
}
