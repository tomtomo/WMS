using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Inbound.Application.Abstractions;

namespace Wms.Inbound.Api.Endpoints;

// GET /v1/goods-receipts/{id}/attachments - metadata
public sealed class ListGRAttachmentsEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        InboundApiRoutes.GoodsReceipts(app)
            .MapGet("/{goodsReceiptId:guid}/attachments", HandleAsync)
            .WithName("ListGRAttachments");

    private static async Task<IResult> HandleAsync(
        Guid goodsReceiptId,
        IGRAttachmentReader reader,
        CancellationToken cancellationToken,
        bool includeInactive = false)
    {
        var attachments = await reader.ListByGoodsReceiptAsync(goodsReceiptId, includeInactive, cancellationToken);
        return Results.Ok(attachments);
    }
}
