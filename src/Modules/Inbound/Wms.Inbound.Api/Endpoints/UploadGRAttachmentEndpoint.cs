using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Inbound.Application.Features.UploadGRAttachment;

namespace Wms.Inbound.Api.Endpoints;

// POST /v1/goods-receipts/{id}/attachments — byte ke IObjectStore, metadata ke DB.
public sealed class UploadGRAttachmentEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        InboundApiRoutes.GoodsReceipts(app)
            .MapPost("/{goodsReceiptId:guid}/attachments", HandleAsync)
            .WithName("UploadGRAttachment")

            // API bearer token tanpa cookie.
            .DisableAntiforgery()
            .WithIdempotencyKey();

    private static async Task<IResult> HandleAsync(
        Guid goodsReceiptId,
        IFormFile file,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        // Length dari server
        var content = file.OpenReadStream();
        await using (content.ConfigureAwait(false))
        {
            var command = new UploadGRAttachmentCommand(
                goodsReceiptId,
                file.FileName,
                file.ContentType,
                file.Length,
                content);

            var result = await sender.Send(command, cancellationToken);
            return result.IsSuccess
                ? Results.Created(
                    $"/v1/goods-receipts/{goodsReceiptId}/attachments/{result.Value}",
                    new UploadGRAttachmentResponse(result.Value))
                : result.ToProblem(httpContext);
        }
    }
}

public sealed record UploadGRAttachmentResponse(Guid AttachmentId);
