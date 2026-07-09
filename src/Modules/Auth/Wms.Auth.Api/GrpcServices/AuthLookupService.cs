using Grpc.Core;
using Wms.Auth.Application.Abstractions;
using Wms.Auth.Grpc.V1;
using Wms.BuildingBlocks.Domain.Results;
using Wms.BuildingBlocks.Web.GrpcInterceptors;

namespace Wms.Auth.Api.GrpcServices;

// Read only gRPC internal auth.v1
public sealed class AuthLookupService(
    IUserReader userReader,
    IRoleReader roleReader,
    IPermissionReader permissionReader)
    : AuthLookup.AuthLookupBase
{
    public override async Task<UserSnapshot> GetUser(GetUserRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (!Guid.TryParse(request.UserId, out var userId))
        {
            throw new ResultFailureException(ResultErrorType.Validation, new Error("user.id_invalid", "userId bukan GUID valid."));
        }

        var user = await userReader.GetByIdAsync(userId, context.CancellationToken);
        if (user is null)
        {
            throw new ResultFailureException(ResultErrorType.NotFound, new Error("user.not_found", "User tidak ditemukan."));
        }

        var snapshot = new UserSnapshot
        {
            UserId = user.UserId.ToString(),
            Username = user.Username,
            Email = user.Email,
            IsActive = user.IsActive,
        };
        snapshot.RoleIds.AddRange(user.RoleIds.Select(id => id.ToString()));
        snapshot.AssignedWarehouseIds.AddRange(user.AssignedWarehouseIds.Select(id => id.ToString()));
        snapshot.PermissionCodes.AddRange(user.PermissionCodes);
        return snapshot;
    }

    public override async Task<RoleSnapshot> GetRole(GetRoleRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (!Guid.TryParse(request.RoleId, out var roleId))
        {
            throw new ResultFailureException(ResultErrorType.Validation, new Error("role.id_invalid", "roleId bukan GUID valid."));
        }

        var role = await roleReader.GetByIdAsync(roleId, context.CancellationToken);
        if (role is null)
        {
            throw new ResultFailureException(ResultErrorType.NotFound, new Error("role.not_found", "Role tidak ditemukan."));
        }

        var snapshot = new RoleSnapshot
        {
            RoleId = role.RoleId.ToString(),
            Code = role.Code,
            Name = role.Name,
            IsActive = role.IsActive,
        };
        snapshot.PermissionCodes.AddRange(role.PermissionCodes);
        return snapshot;
    }

    public override async Task<RoleMembers> GetRoleMembers(GetRoleMembersRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (!Guid.TryParse(request.RoleId, out var roleId))
        {
            throw new ResultFailureException(ResultErrorType.Validation, new Error("role.id_invalid", "roleId bukan GUID valid."));
        }

        var userIds = await userReader.GetUserIdsInRoleAsync(roleId, context.CancellationToken);

        var members = new RoleMembers();
        members.UserIds.AddRange(userIds.Select(id => id.ToString()));
        return members;
    }

    public override async Task<PermissionCatalog> GetPermissions(GetPermissionsRequest request, ServerCallContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        var permissions = await permissionReader.ListAsync(context.CancellationToken);

        var catalog = new PermissionCatalog();
        catalog.Permissions.AddRange(permissions.Select(permission => new PermissionSnapshot
        {
            PermissionId = permission.PermissionId.ToString(),
            Code = permission.Code,
            Description = permission.Description,
        }));
        return catalog;
    }
}
