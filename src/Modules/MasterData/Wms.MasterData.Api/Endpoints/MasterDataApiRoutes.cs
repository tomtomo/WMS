using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Wms.MasterData.Api.Endpoints;

// Group versi /v{n}/warehouses, /v{n}/locations, /v{n}/products.
internal static class MasterDataApiRoutes
{
    public static RouteGroupBuilder Warehouses(IEndpointRouteBuilder app) =>
        Group(app, "warehouses", "/v{version:apiVersion}/warehouses", "Warehouses");

    public static RouteGroupBuilder Locations(IEndpointRouteBuilder app) =>
        Group(app, "locations", "/v{version:apiVersion}/locations", "Locations");

    public static RouteGroupBuilder Products(IEndpointRouteBuilder app) =>
        Group(app, "products", "/v{version:apiVersion}/products", "Products");

    private static RouteGroupBuilder Group(IEndpointRouteBuilder app, string versionSetName, string prefix, string tag)
    {
        var versionSet = app.NewApiVersionSet(versionSetName)
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        return app.MapGroup(prefix)
            .WithApiVersionSet(versionSet)
            .WithTags(tag);
    }
}
