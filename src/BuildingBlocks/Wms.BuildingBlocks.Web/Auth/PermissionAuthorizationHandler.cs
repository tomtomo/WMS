using Microsoft.AspNetCore.Authorization;
using Wms.BuildingBlocks.Application.Abstractions;

namespace Wms.BuildingBlocks.Web.Auth;

// Grant bila klaim 'permission' JWT memuat kode yang diminta.
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        if (context.User.HasClaim(WmsClaimTypes.Permission, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
