using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Application.Features.AssignRole;
using Wms.Auth.Application.Features.AssignWarehouse;
using Wms.Auth.Application.Features.CreateUser;
using Wms.Auth.Application.Features.DisableUser;
using Wms.Auth.Application.Features.LinkExternalLogin;
using Wms.Auth.Application.Features.UnlockUser;
using Wms.BuildingBlocks.Web;

namespace Wms.Auth.Api.Endpoints;

// REST admin /v1/users
public sealed class UserAdminEndpoints : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = AuthApiRoutes.Users(app);
        group.MapGet("/", ListAsync).WithName("ListUsers");
        group.MapGet("/{userId:guid}", GetByIdAsync).WithName("GetUser");
        group.MapPost("/", CreateAsync).WithName("CreateUser").WithIdempotencyKey();
        group.MapPost("/{userId:guid}/roles", AssignRoleAsync).WithName("AssignUserRole").WithIdempotencyKey();
        group.MapPost("/{userId:guid}/warehouses", AssignWarehouseAsync).WithName("AssignUserWarehouse").WithIdempotencyKey();
        group.MapPost("/{userId:guid}/disable", DisableAsync).WithName("DisableUser").WithIdempotencyKey();
        group.MapPost("/{userId:guid}/unlock", UnlockAsync).WithName("UnlockUser").WithIdempotencyKey();
        group.MapPost("/{userId:guid}/external-logins", LinkExternalLoginAsync)
            .WithName("LinkUserExternalLogin").WithIdempotencyKey();
    }

    private static async Task<IResult> ListAsync(
        IUserReader reader,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = 20,
        bool includeInactive = false)
    {
        var result = await reader.ListAsync(page, pageSize, includeInactive, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetByIdAsync(Guid userId, IUserReader reader, CancellationToken cancellationToken)
    {
        var user = await reader.GetByIdAsync(userId, cancellationToken);
        return user is null ? Results.NotFound() : Results.Ok(user);
    }

    private static async Task<IResult> CreateAsync(
        CreateUserRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var command = new CreateUserCommand(
            request.Username,
            request.Email,
            request.Password,
            request.RoleIds ?? [],
            request.AssignedWarehouseIds ?? []);
        var result = await sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/v1/users/{result.Value}", new { userId = result.Value })
            : result.ToProblem(httpContext);
    }

    private static async Task<IResult> AssignRoleAsync(
        Guid userId,
        AssignRoleRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AssignRoleCommand(userId, request.RoleId), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }

    private static async Task<IResult> AssignWarehouseAsync(
        Guid userId,
        AssignWarehouseRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AssignWarehouseCommand(userId, request.WarehouseId), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }

    private static async Task<IResult> DisableAsync(
        Guid userId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DisableUserCommand(userId), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }

    private static async Task<IResult> UnlockAsync(
        Guid userId,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnlockUserCommand(userId), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }

    private static async Task<IResult> LinkExternalLoginAsync(
        Guid userId,
        LinkExternalLoginRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new LinkExternalLoginCommand(userId, request.Provider, request.Subject), cancellationToken);
        return result.IsSuccess
            ? Results.Created($"/v1/users/{userId}/external-logins/{result.Value}", new { externalLoginId = result.Value })
            : result.ToProblem(httpContext);
    }
}

public sealed record CreateUserRequest(
    string Username,
    string Email,
    string Password,
    IReadOnlyList<Guid>? RoleIds,
    IReadOnlyList<Guid>? AssignedWarehouseIds);

public sealed record AssignRoleRequest(Guid RoleId);

public sealed record AssignWarehouseRequest(Guid WarehouseId);

public sealed record LinkExternalLoginRequest(string Provider, string Subject);
