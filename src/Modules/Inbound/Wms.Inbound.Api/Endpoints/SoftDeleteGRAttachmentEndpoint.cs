using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Inbound.Application.Features.SoftDeleteGRAttachment;

namespace Wms.Inbound.Api.Endpoints;

// DELETE /v1/goods-receipts/{id}/attachments/{attachmentId} — soft delete
public sealed class SoftDeleteGRAttachmentEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        InboundApiRoutes.GoodsReceipts(app)
            .MapDelete("/{goodsReceiptId:guid}/attachments/{attachmentId:guid}", HandleAsync)
            .WithName("SoftDeleteGRAttachment");

    private static async Task<IResult> HandleAsync(
        Guid goodsReceiptId,
        Guid attachmentId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SoftDeleteGRAttachmentCommand(goodsReceiptId, attachmentId), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }
}
