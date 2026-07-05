using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Domain;

namespace Wms.MasterData.Application.Features.Location.DeactivateLocation;

internal sealed class DeactivateLocationHandler(ILocationRepository repository)
    : ICommandHandler<DeactivateLocationCommand>
{
    public async Task<Result> Handle(DeactivateLocationCommand command, CancellationToken cancellationToken)
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

        return location.Deactivate();
    }
}
