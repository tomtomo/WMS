using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Outbound.Application.Features.CreateWave;

namespace Wms.Outbound.Api.Endpoints;

// POST /v1/waves — rilis wave dari order backlog.
public sealed class CreateWaveEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        OutboundApiRoutes.Waves(app)
            .MapPost("/", HandleAsync)
            .WithName("CreateWave")
            .WithIdempotencyKey();

    private static async Task<IResult> HandleAsync(
        CreateWaveRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new CreateWaveCommand(request.OrderIds ?? [], request.WarehouseId);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/v1/waves/{result.Value}", new CreateWaveResponse(result.Value))
            : result.ToProblem(httpContext);
    }
}

public sealed record CreateWaveRequest(IReadOnlyList<Guid>? OrderIds, Guid WarehouseId);

public sealed record CreateWaveResponse(Guid WaveId);
