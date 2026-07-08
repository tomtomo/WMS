using Microsoft.AspNetCore.Builder;
using Wms.BuildingBlocks.Web.Auth;

namespace Wms.BuildingBlocks.Web;

// Dipasang setelah authentication agar user sudah terbaca.
public static class IsActiveUserMiddlewareExtensions
{
    public static IApplicationBuilder UseIsActiveUserCheck(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<IsActiveUserMiddleware>();
    }
}
