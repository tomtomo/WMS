using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Application.EventTranslation;
using Wms.Inbound.Domain.ValueObjects;

namespace Wms.Inbound.Application.Features.HoldGoodsReceipt;

internal sealed class HoldGoodsReceiptHandler(
    IGoodsReceiptRepository repository,
    GoodsReceiptEventTranslator translator) : ICommandHandler<HoldGoodsReceiptCommand>
{
    public async Task<Result> Handle(HoldGoodsReceiptCommand command, CancellationToken cancellationToken)
    {
        var loaded = await GoodsReceiptLoader.LoadAsync(repository, command.GoodsReceiptId, cancellationToken);
        if (loaded.IsFailure)
        {
            return loaded;
        }

        var reason = HoldReason.Create(command.Reason);
        if (reason.IsFailure)
        {
            return reason;
        }

        var held = loaded.Value.Hold(reason.Value);
        if (held.IsFailure)
        {
            return held;
        }

        // GoodsReceiptHeld tanpa integration event
        await translator.TranslateAndClearAsync(loaded.Value, cancellationToken);
        return held;
    }
}
