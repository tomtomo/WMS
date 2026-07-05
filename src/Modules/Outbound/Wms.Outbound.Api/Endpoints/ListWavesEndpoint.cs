using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Outbound.Application.Abstractions;

namespace Wms.Outbound.Api.Endpoints;

// GET /v1/waves?warehouseId=&status= — antrean wave per status (default Active).
public sealed class ListWavesEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        OutboundApiRoutes.Waves(app)
            .MapGet("/", HandleAsync)
            .WithName("ListWaves");

    private static async Task<IResult> HandleAsync(
        IWaveListReader reader,
        Guid warehouseId,
        CancellationToken cancellationToken,
        string status = "Active")
    {
        var waves = await reader.GetByStatusAsync(warehouseId, status, cancellationToken);
        return Results.Ok(waves);
    }
}
