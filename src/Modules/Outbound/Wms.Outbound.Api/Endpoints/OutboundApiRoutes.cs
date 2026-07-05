using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Wms.Outbound.Api.Endpoints;

// Group versi /v{n}/waves, /v{n}/picking-tasks, /v{n}/outbound-orders.
internal static class OutboundApiRoutes
{
    public static RouteGroupBuilder Waves(IEndpointRouteBuilder app) =>
        Group(app, "waves", "/v{version:apiVersion}/waves", "Waves");

    public static RouteGroupBuilder PickingTasks(IEndpointRouteBuilder app) =>
        Group(app, "picking-tasks", "/v{version:apiVersion}/picking-tasks", "PickingTasks");

    public static RouteGroupBuilder OutboundOrders(IEndpointRouteBuilder app) =>
        Group(app, "outbound-orders", "/v{version:apiVersion}/outbound-orders", "OutboundOrders");

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
