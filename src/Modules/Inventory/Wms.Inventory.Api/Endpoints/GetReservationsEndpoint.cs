using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Inventory.Application.Abstractions;

namespace Wms.Inventory.Api.Endpoints;

// GET /v1/reservations?waveId= — reservasi hasil alokasi per wave, read langsung ke read port.
public sealed class GetReservationsEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        InventoryApiRoutes.Reservations(app)
            .MapGet("/", HandleAsync)
            .WithName("GetReservations");

    private static async Task<IResult> HandleAsync(
        Guid waveId,
        IStockReservationReader reader,
        CancellationToken cancellationToken)
    {
        var reservations = await reader.GetByWaveAsync(waveId, cancellationToken);
        return Results.Ok(reservations);
    }
}
