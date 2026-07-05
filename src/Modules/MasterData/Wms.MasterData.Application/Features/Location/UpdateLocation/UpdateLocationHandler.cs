using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Domain;
using Wms.MasterData.Domain.Enums;

namespace Wms.MasterData.Application.Features.Location.UpdateLocation;

internal sealed class UpdateLocationHandler(ILocationRepository repository)
    : ICommandHandler<UpdateLocationCommand>
{
    public async Task<Result> Handle(UpdateLocationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = LocationId.Create(command.LocationId);
        if (id.IsFailure)
        {
            return id;
        }

        var location = await repository.GetAsync(id.Value, cancellationToken);
        if (location is null)
        {
            return Result.NotFound(new Error("location.not_found", "Location tidak ditemukan."));
        }

        if (!Enum.TryParse<LocationType>(command.Type, ignoreCase: true, out var type) || !Enum.IsDefined(type))
        {
            return Result.Invalid(new Error("location.type_invalid", "LocationType tidak valid."));
        }

        return location.Update(type, command.Code);
    }
}
