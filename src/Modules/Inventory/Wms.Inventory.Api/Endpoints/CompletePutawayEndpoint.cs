using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Inventory.Application.Features.CompletePutaway;

namespace Wms.Inventory.Api.Endpoints;

// POST /v1/putaway-tasks/{id}/complete
public sealed class CompletePutawayEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        InventoryApiRoutes.PutawayTasks(app)
            .MapPost("/{putawayTaskId:guid}/complete", HandleAsync)
            .WithName("CompletePutaway")
            .WithIdempotencyKey();

    private static async Task<IResult> HandleAsync(
        Guid putawayTaskId,
        CompletePutawayRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new CompletePutawayCommand(putawayTaskId, request.ActualDestinationId, request.OperatorId);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }
}

// placeholder operatorId
public sealed record CompletePutawayRequest(Guid ActualDestinationId, Guid? OperatorId);
