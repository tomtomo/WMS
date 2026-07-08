using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Wms.BuildingBlocks.Web.Auth;

namespace Microsoft.Extensions.DependencyInjection;

// DI authZ endpoint: policy provider dinamis, handler PermissionRequirement
public static class PermissionAuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddAuthorization();
        services.TryAddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.TryAddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
