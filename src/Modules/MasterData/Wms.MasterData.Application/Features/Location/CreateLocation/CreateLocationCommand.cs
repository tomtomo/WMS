using Wms.BuildingBlocks.Application.Abstractions;
using Wms.BuildingBlocks.Application.Messaging;

namespace Wms.MasterData.Application.Features.Location.CreateLocation;

[RequiresPermission(MasterDataPermissions.ManageLocation)]
public sealed record CreateLocationCommand(Guid WarehouseId, string Type, string Code) : ICommand<Guid>;
