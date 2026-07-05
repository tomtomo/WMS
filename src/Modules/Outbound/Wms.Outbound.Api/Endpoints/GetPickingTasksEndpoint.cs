using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Outbound.Application.Abstractions;

namespace Wms.Outbound.Api.Endpoints;

// GET /v1/picking-tasks?assignedTo= — worklist PickingTask Assigned, read langsung ke read port.
public sealed class GetPickingTasksEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        OutboundApiRoutes.PickingTasks(app)
            .MapGet("/", HandleAsync)
            .WithName("GetPickingTasks");

    private static async Task<IResult> HandleAsync(
        IPickingTaskReader reader,
        CancellationToken cancellationToken,
        Guid? assignedTo = null) =>
        Results.Ok(await reader.GetWorklistAsync(assignedTo, cancellationToken));
}
