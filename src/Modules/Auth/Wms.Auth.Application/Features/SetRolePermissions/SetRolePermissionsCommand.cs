using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Auth.Application.Features.SetRolePermissions;

[RequiresPermission(AuthPermissions.AssignPermission)]
public sealed record SetRolePermissionsCommand(Guid RoleId, IReadOnlyList<Guid> PermissionIds) : ICommand;
