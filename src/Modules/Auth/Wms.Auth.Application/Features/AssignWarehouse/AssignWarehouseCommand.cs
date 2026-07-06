using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.Auth.Application.Features.AssignWarehouse;

[RequiresPermission(AuthPermissions.ManageUser)]
public sealed record AssignWarehouseCommand(Guid UserId, Guid WarehouseId) : ICommand;
