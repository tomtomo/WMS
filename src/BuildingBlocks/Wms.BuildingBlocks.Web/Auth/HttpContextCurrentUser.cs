using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.BuildingBlocks.Web.Auth;

// Implement ICurrentUser dari HttpContext: userId dari klaim JWT. SYSTEM saat tak ada user
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private const string JwtSubjectClaim = "sub";

    public string UserId
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            if (principal?.Identity?.IsAuthenticated != true)
            {
                return ICurrentUser.SystemActor;
            }

            return principal.FindFirstValue(JwtSubjectClaim)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? ICurrentUser.SystemActor;
        }
    }

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
