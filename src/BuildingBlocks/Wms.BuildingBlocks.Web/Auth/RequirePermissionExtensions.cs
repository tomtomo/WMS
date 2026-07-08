using Microsoft.AspNetCore.Builder;

namespace Wms.BuildingBlocks.Web;

// Helper untuk menambahkan requirement permission ke endpoint.
public static class RequirePermissionExtensions
{
    public static TBuilder RequirePermission<TBuilder>(this TBuilder builder, string permission)
        where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.RequireAuthorization($"{Auth.PermissionPolicyProvider.PolicyPrefix}{permission}");
    }
}
