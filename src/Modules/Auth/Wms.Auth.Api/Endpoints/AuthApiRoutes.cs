using Asp.Versioning;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Wms.Auth.Api.Endpoints;

// Group versi /v{n} (session) dan /v{n}/{users,roles,permissions} (admin)
internal static class AuthApiRoutes
{
    public static RouteGroupBuilder Auth(IEndpointRouteBuilder app) =>
        Group(app, "auth", "/v{version:apiVersion}", "Auth");

    public static RouteGroupBuilder Users(IEndpointRouteBuilder app) =>
        Group(app, "auth-users", "/v{version:apiVersion}/users", "AuthUsers");

    public static RouteGroupBuilder Roles(IEndpointRouteBuilder app) =>
        Group(app, "auth-roles", "/v{version:apiVersion}/roles", "AuthRoles");

    public static RouteGroupBuilder Permissions(IEndpointRouteBuilder app) =>
        Group(app, "auth-permissions", "/v{version:apiVersion}/permissions", "AuthPermissions");

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
