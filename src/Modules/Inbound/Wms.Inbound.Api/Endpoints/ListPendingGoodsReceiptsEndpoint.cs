using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Inbound.Application.Abstractions;

namespace Wms.Inbound.Api.Endpoints;

// GET /v1/goods-receipts/pending
public sealed class ListPendingGoodsReceiptsEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        InboundApiRoutes.GoodsReceipts(app)
            .MapGet("/pending", HandleAsync)
            .WithName("ListPendingGoodsReceipts");

    private static async Task<IResult> HandleAsync(
        IGoodsReceiptListReader reader,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20)
    {
        var result = await reader.ListPendingAsync(page, pageSize, cancellationToken);
        return Results.Ok(result);
    }
}
