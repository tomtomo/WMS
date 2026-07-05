using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.MasterData.Application.Features.Location.DeactivateLocation;

// Soft delete Location
[RequiresPermission(MasterDataPermissions.ManageLocation)]
public sealed record DeactivateLocationCommand(Guid LocationId) : ICommand;
