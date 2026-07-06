using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Auth.Application.Features.DisableUser;

[RequiresPermission(AuthPermissions.ManageUser)]
public sealed record DisableUserCommand(Guid UserId) : ICommand;
