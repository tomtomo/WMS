using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Outbound.Application.Features.CompletePickingTask;

namespace Wms.Outbound.Api.Endpoints;

// POST /v1/picking-tasks/{taskId}/complete — operator menyelesaikan picking.
public sealed class CompletePickingTaskEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        OutboundApiRoutes.PickingTasks(app)
            .MapPost("/{taskId:guid}/complete", HandleAsync)
            .WithName("CompletePickingTask")
            .WithIdempotencyKey();

    private static async Task<IResult> HandleAsync(
        Guid taskId,
        CompletePickingTaskRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new CompletePickingTaskCommand(taskId, request.ActualQty, request.StagingLocationId, request.OperatorId);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }
}

public sealed record CompletePickingTaskRequest(decimal ActualQty, Guid StagingLocationId, Guid? OperatorId);
