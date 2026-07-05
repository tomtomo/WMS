using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Application.Features.Warehouse.CreateWarehouse;
using Wms.MasterData.Application.Features.Warehouse.DeactivateWarehouse;
using Wms.MasterData.Application.Features.Warehouse.UpdateWarehouse;

namespace Wms.MasterData.Api.Endpoints;

// REST management /v1/warehouses
public sealed class WarehouseEndpoints : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = MasterDataApiRoutes.Warehouses(app);
        group.MapGet("/", ListAsync).WithName("ListWarehouses");
        group.MapGet("/{warehouseId:guid}", GetByIdAsync).WithName("GetWarehouse");
        group.MapPost("/", CreateAsync).WithName("CreateWarehouse");
        group.MapPut("/{warehouseId:guid}", UpdateAsync).WithName("UpdateWarehouse");
        group.MapDelete("/{warehouseId:guid}", DeactivateAsync).WithName("DeactivateWarehouse");
    }

    private static async Task<IResult> ListAsync(
        IWarehouseReader reader,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        bool includeInactive = false)
    {
        var result = await reader.ListAsync(page, pageSize, includeInactive, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetByIdAsync(Guid warehouseId, IWarehouseReader reader, CancellationToken cancellationToken)
    {
        var warehouse = await reader.GetByIdAsync(warehouseId, cancellationToken);
        return warehouse is null ? Results.NotFound() : Results.Ok(warehouse);
    }

    private static async Task<IResult> CreateAsync(
        CreateWarehouseRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateWarehouseCommand(request.Name, request.Address), cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/v1/warehouses/{result.Value}", new { warehouseId = result.Value })
            : result.ToProblem(httpContext);
    }

    private static async Task<IResult> UpdateAsync(
        Guid warehouseId,
        UpdateWarehouseRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UpdateWarehouseCommand(warehouseId, request.Name, request.Address), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }

    private static async Task<IResult> DeactivateAsync(
        Guid warehouseId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateWarehouseCommand(warehouseId), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }
}

public sealed record CreateWarehouseRequest(string Name, string Address);

public sealed record UpdateWarehouseRequest(string Name, string Address);
