using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Wms.Notifications.Endpoints;

// Route group ber versi REST inbox in app — /v{n}/inbox
internal static class NotificationsApiRoutes
{
    public static RouteGroupBuilder Inbox(IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet("inbox")
            .HasApiVersion(new ApiVersion(1, 0))
            .ReportApiVersions()
            .Build();

        return app.MapGroup("/v{version:apiVersion}/inbox")
            .WithApiVersionSet(versionSet)
            .WithTags("Inbox");
    }
}
