using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Domain;
using Wms.MasterData.Domain.Enums;
using DomainLocation = Wms.MasterData.Domain.Location;

namespace Wms.MasterData.Application.Features.Location.CreateLocation;

internal sealed class CreateLocationHandler(ILocationRepository repository)
    : ICommandHandler<CreateLocationCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateLocationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var warehouseId = WarehouseId.Create(command.WarehouseId);
        if (warehouseId.IsFailure)
        {
            return warehouseId.ForwardFailure<Guid>();
        }

        if (!await repository.WarehouseExistsAsync(warehouseId.Value, cancellationToken))
        {
            return Result.Invalid<Guid>(new Error("location.warehouse_unknown", "WarehouseId tidak dikenal di Master Data."));
        }

        if (!Enum.TryParse<LocationType>(command.Type, ignoreCase: true, out var type) || !Enum.IsDefined(type))
        {
            return Result.Invalid<Guid>(new Error("location.type_invalid", "LocationType tidak valid."));
        }

        var id = LocationId.Create(Guid.NewGuid());
        var location = DomainLocation.Create(id.Value, warehouseId.Value, type, command.Code);
        if (location.IsFailure)
        {
            return location.ForwardFailure<Guid>();
        }

        await repository.AddAsync(location.Value, cancellationToken);
        return Result.Success(location.Value.Id.Value);
    }
}
