using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Inbound.Application.Features.CreateGoodsReceiptHeader;

namespace Wms.Inbound.Api.Endpoints;

// POST /v1/goods-receipts
public sealed class CreateGoodsReceiptEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        InboundApiRoutes.GoodsReceipts(app)
            .MapPost("/", HandleAsync)
            .WithName("CreateGoodsReceipt")
            .WithIdempotencyKey();

    private static async Task<IResult> HandleAsync(
        CreateGoodsReceiptRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new CreateGoodsReceiptHeaderCommand(
            request.PoRef,
            request.SupplierId,
            request.WarehouseId,
            request.DockDoor,
            [.. (request.ExpectedLines ?? []).Select(line => new ExpectedLineInput(line.Sku, line.ExpectedQty, line.Uom))]);

        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/v1/goods-receipts/{result.Value}", new CreateGoodsReceiptResponse(result.Value))
            : result.ToProblem(httpContext);
    }
}

public sealed record CreateGoodsReceiptRequest(
    string PoRef,
    Guid SupplierId,
    Guid WarehouseId,
    string DockDoor,
    IReadOnlyList<ExpectedLineRequest>? ExpectedLines);

public sealed record ExpectedLineRequest(string Sku, decimal ExpectedQty, string Uom);

public sealed record CreateGoodsReceiptResponse(Guid GoodsReceiptId);
