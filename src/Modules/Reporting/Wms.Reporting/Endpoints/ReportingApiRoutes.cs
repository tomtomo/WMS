using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Wms.Reporting.Endpoints;

// Route group ber versi REST report — /v{n}/reports
internal static class ReportingApiRoutes
{
    public static RouteGroupBuilder Reports(IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet("reports")
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        return app.MapGroup("/v{version:apiVersion}/reports")
            .WithApiVersionSet(versionSet)
            .WithTags("Reports");
    }

    // Route group ber versi telemetry operasional — /v{n}/telemetry
    public static RouteGroupBuilder Telemetry(IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet("telemetry")
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        return app.MapGroup("/v{version:apiVersion}/telemetry")
            .WithApiVersionSet(versionSet)
            .WithTags("Telemetry");
    }
}
