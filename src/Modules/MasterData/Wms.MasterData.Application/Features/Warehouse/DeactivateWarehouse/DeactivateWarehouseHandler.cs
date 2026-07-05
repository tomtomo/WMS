using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Domain;

namespace Wms.MasterData.Application.Features.Warehouse.DeactivateWarehouse;

internal sealed class DeactivateWarehouseHandler(IWarehouseRepository repository)
    : ICommandHandler<DeactivateWarehouseCommand>
{
    public async Task<Result> Handle(DeactivateWarehouseCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = WarehouseId.Create(command.WarehouseId);
        if (id.IsFailure)
        {
            return id;
        }

        var warehouse = await repository.GetAsync(id.Value, cancellationToken);
        if (warehouse is null)
        {
            return Result.NotFound(new Error("warehouse.not_found", "Warehouse tidak ditemukan."));
        }

        return warehouse.Deactivate();
    }
}
