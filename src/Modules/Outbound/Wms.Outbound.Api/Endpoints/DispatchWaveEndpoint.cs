using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Outbound.Application.Features.DispatchWave;

namespace Wms.Outbound.Api.Endpoints;

// POST /v1/waves/{waveId}/dispatch — dispatch wave.
public sealed class DispatchWaveEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        OutboundApiRoutes.Waves(app)
            .MapPost("/{waveId:guid}/dispatch", HandleAsync)
            .WithName("DispatchWave")
            .WithIdempotencyKey();

    private static async Task<IResult> HandleAsync(
        Guid waveId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DispatchWaveCommand(waveId), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }
}
