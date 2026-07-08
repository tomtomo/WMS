using Microsoft.AspNetCore.Authorization;

namespace Wms.BuildingBlocks.Web.Auth;

// Requirement policy endpoint: caller wajib punya klaim permission Module.Action.
public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
