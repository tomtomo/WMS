using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.Auth.Application.Abstractions;
using Wms.BuildingBlocks.Web;

namespace Wms.Auth.Api.Endpoints;

// REST katalog /v1/permissions
public sealed class PermissionEndpoints : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = AuthApiRoutes.Permissions(app);
        group.MapGet("/", ListAsync).WithName("ListPermissions");
    }

    private static async Task<IResult> ListAsync(IPermissionReader reader, CancellationToken cancellationToken)
    {
        var permissions = await reader.ListAsync(cancellationToken);
        return Results.Ok(permissions);
    }
}
