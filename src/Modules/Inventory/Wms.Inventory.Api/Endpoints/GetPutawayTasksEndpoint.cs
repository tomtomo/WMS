using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Inventory.Application.Abstractions;

namespace Wms.Inventory.Api.Endpoints;

// GET /v1/putaway-tasks?assignedTo= — antrean PutawayTask Assigned, read langsung ke read port.
public sealed class GetPutawayTasksEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        InventoryApiRoutes.PutawayTasks(app)
            .MapGet("/", HandleAsync)
            .WithName("GetPutawayTasks");

    private static async Task<IResult> HandleAsync(
        IPutawayTaskReader reader,
        CancellationToken cancellationToken,
        Guid? assignedTo = null)
    {
        var tasks = await reader.GetQueueAsync(assignedTo, cancellationToken);
        return Results.Ok(tasks);
    }
}
