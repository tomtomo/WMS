using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Wms.Auth.Application.Features.Login;
using Wms.Auth.Application.Features.Logout;
using Wms.Auth.Application.Features.RefreshAccessToken;
using Wms.BuildingBlocks.Web;

namespace Wms.Auth.Api.Endpoints;

// REST session /v1: login, refresh, logout.
public sealed class AuthEndpoints : IEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app)
    {
        var group = AuthApiRoutes.Auth(app);

        // Endpoint sesi harus bisa diakses tanpa bearer token,
        // karena kredensial dikirim lewat body saat login/refresh/logout.
        group.AllowAnonymous();

        group.MapPost("/login", LoginAsync).WithName("Login").WithIdempotencyKey();
        group.MapPost("/refresh", RefreshAsync).WithName("RefreshAccessToken").WithIdempotencyKey();
        group.MapPost("/logout", LogoutAsync).WithName("Logout").WithIdempotencyKey();
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(request.Username, request.Password), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new TokenResponse(result.Value.AccessToken, result.Value.ExpiresAt, result.Value.RefreshToken))
            : result.ToProblem(httpContext);
    }

    private static async Task<IResult> RefreshAsync(
        RefreshRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new RefreshAccessTokenCommand(request.RefreshToken), cancellationToken);
        return result.IsSuccess
            ? Results.Ok(new TokenResponse(result.Value.AccessToken, result.Value.ExpiresAt, result.Value.RefreshToken))
            : result.ToProblem(httpContext);
    }

    private static async Task<IResult> LogoutAsync(
        RefreshRequest request,
        ISender sender,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LogoutCommand(request.RefreshToken), cancellationToken);
        return result.IsSuccess ? Results.NoContent() : result.ToProblem(httpContext);
    }
}

public sealed record LoginRequest(string Username, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record TokenResponse(string AccessToken, DateTimeOffset ExpiresAt, string RefreshToken);
