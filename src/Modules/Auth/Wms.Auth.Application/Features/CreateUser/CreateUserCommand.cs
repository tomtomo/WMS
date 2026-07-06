using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Auth.Application.Features.CreateUser;

[RequiresPermission(AuthPermissions.ManageUser)]
public sealed record CreateUserCommand(
    string Username,
    string Email,
    string Password,
    IReadOnlyList<Guid> RoleIds,
    IReadOnlyList<Guid> AssignedWarehouseIds) : ICommand<Guid>;
