using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Application.Features.Location.CreateLocation;
using Wms.MasterData.Application.Features.Location.DeactivateLocation;
using Wms.MasterData.Application.Features.Location.UpdateLocation;

namespace Wms.MasterData.Api.Endpoints;

// REST management /v1/locations
public sealed class LocationEndpoints : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = MasterDataApiRoutes.Locations(app);
        group.MapGet("/", ListAsync).WithName("ListLocations");
        group.MapGet("/{locationId:guid}", GetByIdAsync).WithName("GetLocation");
        group.MapPost("/", CreateAsync).WithName("CreateLocation");
        group.MapPut("/{locationId:guid}", UpdateAsync).WithName("UpdateLocation");
        group.MapDelete("/{locationId:guid}", DeactivateAsync).WithName("DeactivateLocation");
    }

    private static async Task<IResult> ListAsync(
        ILocationReader reader,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        bool includeInactive = false)
    {
        var result = await reader.ListAsync(page, pageSize, includeInactive, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetByIdAsync(Guid locationId, ILocationReader reader, CancellationToken cancellationToken)
    {
        var location = await reader.GetByIdAsync(locationId, cancellationToken);
        return location is null ? Results.NotFound() : Results.Ok(location);
    }

    private static async Task<IResult> CreateAsync(
        CreateLocationRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CreateLocationCommand(request.WarehouseId, request.Type, request.Code), cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/v1/locations/{result.Value}", new { locationId = result.Value })
            : result.ToProblem(httpContext);
    }

    private static async Task<IResult> UpdateAsync(
        Guid locationId,
        UpdateLocationRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateLocationCommand(locationId, request.Type, request.Code), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }

    private static async Task<IResult> DeactivateAsync(
        Guid locationId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateLocationCommand(locationId), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }
}

public sealed record CreateLocationRequest(Guid WarehouseId, string Type, string Code);

public sealed record UpdateLocationRequest(string Type, string Code);
