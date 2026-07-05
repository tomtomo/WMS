using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.MasterData.Application.Abstractions;
using Wms.MasterData.Domain;
using DomainWarehouse = Wms.MasterData.Domain.Warehouse;

namespace Wms.MasterData.Application.Features.Warehouse.CreateWarehouse;

internal sealed class CreateWarehouseHandler(IWarehouseRepository repository)
    : ICommandHandler<CreateWarehouseCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateWarehouseCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var id = WarehouseId.Create(Guid.NewGuid());
        var warehouse = DomainWarehouse.Create(id.Value, command.Name, command.Address);
        if (warehouse.IsFailure)
        {
            return warehouse.ForwardFailure<Guid>();
        }

        await repository.AddAsync(warehouse.Value, cancellationToken);
        return Result.Success(warehouse.Value.Id.Value);
    }
}
