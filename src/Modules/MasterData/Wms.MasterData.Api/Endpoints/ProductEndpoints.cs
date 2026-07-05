using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.BuildingBlocks.Web;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Application.Features.Product.CreateProduct;
using Wms.MasterData.Application.Features.Product.DeactivateProduct;
using Wms.MasterData.Application.Features.Product.UpdateProduct;

namespace Wms.MasterData.Api.Endpoints;

// REST management /v1/products
public sealed class ProductEndpoints : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = MasterDataApiRoutes.Products(app);
        group.MapGet("/", ListAsync).WithName("ListProducts");
        group.MapGet("/{sku}", GetBySkuAsync).WithName("GetProduct");
        group.MapPost("/", CreateAsync).WithName("CreateProduct");
        group.MapPut("/{sku}", UpdateAsync).WithName("UpdateProduct");
        group.MapDelete("/{sku}", DeactivateAsync).WithName("DeactivateProduct");
    }

    private static async Task<IResult> ListAsync(
        IProductReader reader,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        bool includeInactive = false)
    {
        var result = await reader.ListAsync(page, pageSize, includeInactive, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetBySkuAsync(string sku, IProductReader reader, CancellationToken cancellationToken)
    {
        var product = await reader.GetBySkuAsync(sku, cancellationToken);
        return product is null ? Results.NotFound() : Results.Ok(product);
    }

    private static async Task<IResult> CreateAsync(
        CreateProductRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new CreateProductCommand(
            request.Sku,
            request.Name,
            request.Uom,
            request.BatchTrackingRequired,
            request.ExpiryTrackingRequired,
            request.QcRequiredOnReceipt,
            request.ShelfLifeDays);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/v1/products/{result.Value}", new { sku = result.Value })
            : result.ToProblem(httpContext);
    }

    private static async Task<IResult> UpdateAsync(
        string sku,
        UpdateProductRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProductCommand(
            sku,
            request.Name,
            request.Uom,
            request.BatchTrackingRequired,
            request.ExpiryTrackingRequired,
            request.QcRequiredOnReceipt,
            request.ShelfLifeDays);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }

    private static async Task<IResult> DeactivateAsync(
        string sku,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateProductCommand(sku), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }
}

public sealed record CreateProductRequest(
    string Sku,
    string Name,
    string Uom,
    bool BatchTrackingRequired,
    bool ExpiryTrackingRequired,
    bool QcRequiredOnReceipt,
    int? ShelfLifeDays);

public sealed record UpdateProductRequest(
    string Name,
    string Uom,
    bool BatchTrackingRequired,
    bool ExpiryTrackingRequired,
    bool QcRequiredOnReceipt,
    int? ShelfLifeDays);
