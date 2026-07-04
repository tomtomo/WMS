using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Wms.Inventory.Api.Endpoints;

// Group versi /v{n}/stock dan /v{n}/putaway-tasks.
internal static class InventoryApiRoutes
{
    public static RouteGroupBuilder Stock(IEndpointRouteBuilder app) =>
        Group(app, "stock", "/v{version:apiVersion}/stock", "Stock");

    public static RouteGroupBuilder PutawayTasks(IEndpointRouteBuilder app) =>
        Group(app, "putaway-tasks", "/v{version:apiVersion}/putaway-tasks", "PutawayTasks");

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
