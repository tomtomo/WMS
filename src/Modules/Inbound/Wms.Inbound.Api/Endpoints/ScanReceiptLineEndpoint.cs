using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Web;
using Wms.Inbound.Application.Features.ScanReceiptLine;
using Wms.Inbound.Domain.Enums;

namespace Wms.Inbound.Api.Endpoints;

// POST /v1/goods-receipts/{id}/scans.
public sealed class ScanReceiptLineEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        InboundApiRoutes.GoodsReceipts(app)
            .MapPost("/{goodsReceiptId:guid}/scans", HandleAsync)
            .WithName("ScanReceiptLine")
            .WithIdempotencyKey();

    private static async Task<IResult> HandleAsync(
        Guid goodsReceiptId,
        ScanReceiptLineRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(request.LineStatus, ignoreCase: true, out LineStatus lineStatus)
            || !Enum.IsDefined(lineStatus))
        {
            return Result.Invalid(new Error(
                "goods_receipt.line_status_invalid",
                "lineStatus harus salah satu dari: Good, WrongItem, QcHold.")).ToProblem(httpContext);
        }

        var command = new ScanReceiptLineCommand(
            goodsReceiptId,
            request.Sku,
            request.ActualQty,
            request.Batch,
            request.Expiry,
            lineStatus);

        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }
}

public sealed record ScanReceiptLineRequest(
    string Sku,
    decimal ActualQty,
    string? Batch,
    DateOnly? Expiry,
    string LineStatus);
