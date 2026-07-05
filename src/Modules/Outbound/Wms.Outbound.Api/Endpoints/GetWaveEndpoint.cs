using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Outbound.Application.Abstractions;

namespace Wms.Outbound.Api.Endpoints;

// GET /v1/waves/{waveId} — detail wave
public sealed class GetWaveEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        OutboundApiRoutes.Waves(app)
            .MapGet("/{waveId:guid}", HandleAsync)
            .WithName("GetWave");

    private static async Task<IResult> HandleAsync(Guid waveId, IWaveReader reader, CancellationToken cancellationToken)
    {
        var wave = await reader.GetByIdAsync(waveId, cancellationToken);
        return wave is null ? Results.NotFound() : Results.Ok(wave);
    }
}
