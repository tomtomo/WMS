using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.Inventory.Application.Abstractions;

namespace Wms.Inventory.Api.Endpoints;

// GET /v1/stock?warehouseId=&sku= — balance Available, read langsung ke read-port.
public sealed class GetStockEndpoint : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        InventoryApiRoutes.Stock(app)
            .MapGet("/", HandleAsync)
            .WithName("GetStock");

    private static async Task<IResult> HandleAsync(
        Guid warehouseId,
        IStockReader reader,
        CancellationToken cancellationToken,
        string? sku = null)
    {
        var balances = await reader.GetAvailableAsync(warehouseId, sku, cancellationToken);
        return Results.Ok(balances);
    }
}
