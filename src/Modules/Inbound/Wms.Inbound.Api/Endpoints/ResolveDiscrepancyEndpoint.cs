using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Web;
using Wms.Inbound.Application.Features.ResolveDiscrepancy;
using Wms.Inbound.Domain.Enums;

namespace Wms.Inbound.Api.Endpoints;

// POST /v1/goods-receipts/{id}/discrepancies/{discrepancyId}/resolution
public sealed class ResolveDiscrepancyEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        InboundApiRoutes.GoodsReceipts(app)
            .MapPost("/{goodsReceiptId:guid}/discrepancies/{discrepancyId:guid}/resolution", HandleAsync)
            .WithName("ResolveDiscrepancy")
            .WithIdempotencyKey();

    private static async Task<IResult> HandleAsync(
        Guid goodsReceiptId,
        Guid discrepancyId,
        ResolveDiscrepancyRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse(request.Action, ignoreCase: true, out ResolutionAction action)
            || !Enum.IsDefined(action))
        {
            return Result.Invalid(new Error(
                "goods_receipt.resolution_action_invalid",
                "action harus salah satu dari: AcceptPartial, RejectExcess, ReturnToSupplier, SendToQC.")).ToProblem(httpContext);
        }

        var command = new ResolveDiscrepancyCommand(goodsReceiptId, discrepancyId, action, request.Note);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }
}

public sealed record ResolveDiscrepancyRequest(string Action, string? Note);
