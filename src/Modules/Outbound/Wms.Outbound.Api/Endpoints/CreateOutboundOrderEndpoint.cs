using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Outbound.Application.Features.CreateOutboundOrder;

namespace Wms.Outbound.Api.Endpoints;

// POST /v1/outbound-orders
public sealed class CreateOutboundOrderEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        OutboundApiRoutes.OutboundOrders(app)
            .MapPost("/", HandleAsync)
            .WithName("CreateOutboundOrder")
            .WithIdempotencyKey();

    private static async Task<IResult> HandleAsync(
        CreateOutboundOrderRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new CreateOutboundOrderCommand(
            request.CustomerId,
            request.Recipient,
            request.AddressLine,
            request.City,
            [.. (request.Lines ?? []).Select(line => new CreateOutboundOrderLine(line.Sku, line.Qty, line.Uom))]);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/v1/outbound-orders/{result.Value}", new CreateOutboundOrderResponse(result.Value))
            : result.ToProblem(httpContext);
    }
}

public sealed record CreateOutboundOrderRequest(
    Guid CustomerId,
    string Recipient,
    string AddressLine,
    string City,
    IReadOnlyList<OutboundOrderLineRequest>? Lines);

public sealed record OutboundOrderLineRequest(string Sku, decimal Qty, string Uom);

public sealed record CreateOutboundOrderResponse(Guid OrderId);
