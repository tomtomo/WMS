using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Outbound.Application.Abstractions;

namespace Wms.Outbound.Api.Endpoints;

// GET /v1/outbound-orders/{orderId}
public sealed class GetOutboundOrderEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        OutboundApiRoutes.OutboundOrders(app)
            .MapGet("/{orderId:guid}", HandleAsync)
            .WithName("GetOutboundOrder");

    private static async Task<IResult> HandleAsync(Guid orderId, IOutboundOrderReader reader, CancellationToken cancellationToken)
    {
        var order = await reader.GetByIdAsync(orderId, cancellationToken);
        return order is null ? Results.NotFound() : Results.Ok(order);
    }
}
