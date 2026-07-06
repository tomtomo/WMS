using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Auth.Application.Features.UnlockUser;

[RequiresPermission(AuthPermissions.ManageUser)]
public sealed record UnlockUserCommand(Guid UserId) : ICommand;
