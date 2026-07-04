using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Application.Abstractions;

namespace Wms.Inbound.Application.Features.ResolveDiscrepancy;

internal sealed class ResolveDiscrepancyHandler(IGoodsReceiptRepository repository)
    : ICommandHandler<ResolveDiscrepancyCommand>
{
    public async Task<Result> Handle(ResolveDiscrepancyCommand command, CancellationToken cancellationToken)
    {
        var loaded = await GoodsReceiptLoader.LoadAsync(repository, command.GoodsReceiptId, cancellationToken);
        if (loaded.IsFailure)
        {
            return loaded;
        }

        return loaded.Value.Resolve(command.DiscrepancyId, command.Action, command.Note);
    }
}
