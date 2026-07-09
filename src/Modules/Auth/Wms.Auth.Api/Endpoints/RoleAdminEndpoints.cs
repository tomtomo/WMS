using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Application.Features.CreateRole;
using Wms.Auth.Application.Features.SetRolePermissions;
using Wms.BuildingBlocks.Web;

namespace Wms.Auth.Api.Endpoints;

// REST admin /v1/roles
public sealed class RoleAdminEndpoints : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = AuthApiRoutes.Roles(app);
        group.MapGet("/", ListAsync).WithName("ListRoles");
        group.MapGet("/{roleId:guid}", GetByIdAsync).WithName("GetRole");
        group.MapPost("/", CreateAsync).WithName("CreateRole").WithIdempotencyKey();
        group.MapPut("/{roleId:guid}/permissions", SetPermissionsAsync).WithName("SetRolePermissions").WithIdempotencyKey();
    }

    private static async Task<IResult> ListAsync(
        IRoleReader reader,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        bool includeInactive = false)
    {
        var result = await reader.ListAsync(page, pageSize, includeInactive, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetByIdAsync(Guid roleId, IRoleReader reader, CancellationToken cancellationToken)
    {
        var role = await reader.GetByIdAsync(roleId, cancellationToken);
        return role is null ? Results.NotFound() : Results.Ok(role);
    }

    private static async Task<IResult> CreateAsync(
        CreateRoleRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new CreateRoleCommand(request.Code, request.Name, request.PermissionIds ?? []);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/v1/roles/{result.Value}", new { roleId = result.Value })
            : result.ToProblem(httpContext);
    }

    private static async Task<IResult> SetPermissionsAsync(
        Guid roleId,
        SetRolePermissionsRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new SetRolePermissionsCommand(roleId, request.PermissionIds ?? []), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }
}

public sealed record CreateRoleRequest(string Code, string Name, IReadOnlyList<Guid>? PermissionIds);

public sealed record SetRolePermissionsRequest(IReadOnlyList<Guid>? PermissionIds);
