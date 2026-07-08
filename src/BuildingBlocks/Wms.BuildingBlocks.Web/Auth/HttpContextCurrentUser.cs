using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Abstractions.Ports;

namespace Wms.BuildingBlocks.Web.Auth;

// Implement ICurrentUser dari HttpContext: userId, permission, warehouse dari klaim JWT. SYSTEM saat tidak ada user.
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    // Scoped per request: klaim diparse sekali lalu dicache.
    private IReadOnlyCollection<string>? _permissions;
    private IReadOnlyCollection<Guid>? _assignedWarehouseIds;

    public string UserId
    {
        get
        {
            var principal = AuthenticatedPrincipal;
            if (principal is null)
            {
                return ICurrentUser.SystemActor;
            }

            return principal.FindFirstValue(WmsClaimTypes.Subject)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? ICurrentUser.SystemActor;
        }
    }

    public bool IsAuthenticated => AuthenticatedPrincipal is not null;

    public IReadOnlyCollection<string> Permissions => _permissions ??= ReadPermissions();

    public IReadOnlyCollection<Guid> AssignedWarehouseIds => _assignedWarehouseIds ??= ReadAssignedWarehouseIds();

    // Principal jika terautentikasi.null saat tidak ada HttpContext (worker/consumer)
    private ClaimsPrincipal? AuthenticatedPrincipal
    {
        get
        {
            var principal = httpContextAccessor.HttpContext?.User;
            return principal?.Identity?.IsAuthenticated == true ? principal : null;
        }
    }

    private IReadOnlyCollection<string> ReadPermissions() =>
        AuthenticatedPrincipal is { } principal
            ? principal.FindAll(WmsClaimTypes.Permission).Select(claim => claim.Value).ToArray()
            : [];

    private IReadOnlyCollection<Guid> ReadAssignedWarehouseIds() =>
        AuthenticatedPrincipal is { } principal
            ? principal.FindAll(WmsClaimTypes.Warehouse)
                .Select(claim => Guid.TryParse(claim.Value, out var id) ? id : (Guid?)null)
                .OfType<Guid>()
                .ToArray()
            : [];
}
