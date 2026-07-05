using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Outbound.Application.Abstractions;

namespace Wms.Outbound.Api.Endpoints;

// GET /v1/outbound-orders/backlog — order backlog (New) dengan per line allocationStatus.
public sealed class GetOutboundBacklogEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        OutboundApiRoutes.OutboundOrders(app)
            .MapGet("/backlog", HandleAsync)
            .WithName("GetOutboundBacklog");

    private static async Task<IResult> HandleAsync(IOutboundOrderReader reader, CancellationToken cancellationToken) =>
        Results.Ok(await reader.GetBacklogAsync(cancellationToken));
}
