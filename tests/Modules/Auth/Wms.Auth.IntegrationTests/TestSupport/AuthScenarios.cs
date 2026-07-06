using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Wms.Auth.Application.Features.CreateRole;
using Wms.Auth.Application.Features.CreateUser;
using Wms.Auth.Application.Features.DisableUser;
using Wms.Auth.Application.Features.Login;
using Wms.Auth.Application.Features.Logout;
using Wms.Auth.Application.Features.RefreshAccessToken;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.IntegrationTests.TestSupport;

// Helper untuk menyiapkan data Auth.
internal static class AuthScenarios
{
    public static async Task<Guid> CreateUserAsync(
        IServiceProvider provider,
        string username,
        string password,
        IReadOnlyList<Guid>? roleIds = null,
        IReadOnlyList<Guid>? warehouseIds = null)
    {
        using var scope = provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new CreateUserCommand(username, $"{username}@wms.local", password, roleIds ?? [], warehouseIds ?? []));
        return result.Value;
    }

    public static async Task<Guid> CreateRoleAsync(
        IServiceProvider provider,
        string code,
        string name,
        IReadOnlyList<Guid>? permissionIds = null)
    {
        using var scope = provider.CreateScope();
        var result = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new CreateRoleCommand(code, name, permissionIds ?? []));
        return result.Value;
    }

    public static async Task DisableUserAsync(IServiceProvider provider, Guid userId)
    {
        using var scope = provider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(new DisableUserCommand(userId));
    }

    public static async Task<Result<LoginResponse>> LoginAsync(IServiceProvider provider, string username, string password)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new LoginCommand(username, password));
    }

    public static async Task<Result<RefreshAccessTokenResponse>> RefreshAsync(IServiceProvider provider, string refreshToken)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new RefreshAccessTokenCommand(refreshToken));
    }

    public static async Task<Result> LogoutAsync(IServiceProvider provider, string refreshToken)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new LogoutCommand(refreshToken));
    }
}
