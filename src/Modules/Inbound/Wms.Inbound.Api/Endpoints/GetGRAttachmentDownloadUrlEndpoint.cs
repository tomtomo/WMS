using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Inbound.Application.Features.GetGRAttachmentDownloadUrl;

namespace Wms.Inbound.Api.Endpoints;

// GET /v1/goods-receipts/{id}/attachments/{attachmentId}/download-url
public sealed class GetGRAttachmentDownloadUrlEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        InboundApiRoutes.GoodsReceipts(app)
            .MapGet("/{goodsReceiptId:guid}/attachments/{attachmentId:guid}/download-url", HandleAsync)
            .WithName("GetGRAttachmentDownloadUrl");

    private static async Task<IResult> HandleAsync(
        Guid goodsReceiptId,
        Guid attachmentId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetGRAttachmentDownloadUrlQuery(goodsReceiptId, attachmentId), cancellationToken);
        return result.IsSuccess ? Results.Ok(result.Value) : result.ToProblem(httpContext);
    }
}
