using Wms.Auth.Application.Abstractions;
using Wms.Auth.Domain;
using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;

namespace Wms.Auth.Application.Features.SetRolePermissions;

internal sealed class SetRolePermissionsHandler(IRoleRepository roleRepository)
    : ICommandHandler<SetRolePermissionsCommand>
{
    public async Task<Result> Handle(SetRolePermissionsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var roleId = RoleId.Create(command.RoleId);
        if (roleId.IsFailure)
        {
            return roleId;
        }

        var role = await roleRepository.GetAsync(roleId.Value, cancellationToken);
        if (role is null)
        {
            return Result.NotFound(new Error("role.not_found", "Role tidak ditemukan."));
        }

        return role.SetPermissions(command.PermissionIds);
    }
}
