using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Auth.Application.Features.AssignRole;

[RequiresPermission(AuthPermissions.ManageUser)]
public sealed record AssignRoleCommand(Guid UserId, Guid RoleId) : ICommand;
