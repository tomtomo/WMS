using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.MasterData.Application.Features.Location.UpdateLocation;

[RequiresPermission(MasterDataPermissions.ManageLocation)]
public sealed record UpdateLocationCommand(Guid LocationId, string Type, string Code) : ICommand;
