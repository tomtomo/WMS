using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Auth.Application.Features.CreateRole;

[RequiresPermission(AuthPermissions.ManageRole)]
public sealed record CreateRoleCommand(string Code, string Name, IReadOnlyList<Guid> PermissionIds) : ICommand<Guid>;
