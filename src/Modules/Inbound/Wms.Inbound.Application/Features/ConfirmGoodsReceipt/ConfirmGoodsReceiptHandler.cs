using Wms.BuildingBlocks.Application.Messaging;
using Wms.BuildingBlocks.Domain.Results;
using Wms.Inbound.Application.Abstractions;
using Wms.Inbound.Application.EventTranslation;

namespace Wms.Inbound.Application.Features.ConfirmGoodsReceipt;

internal sealed class ConfirmGoodsReceiptHandler(
    IGoodsReceiptRepository repository,
    GoodsReceiptEventTranslator translator) : ICommandHandler<ConfirmGoodsReceiptCommand>
{
    public async Task<Result> Handle(ConfirmGoodsReceiptCommand command, CancellationToken cancellationToken)
    {
        var loaded = await GoodsReceiptLoader.LoadAsync(repository, command.GoodsReceiptId, cancellationToken);
        if (loaded.IsFailure)
        {
            return loaded;
        }

        var confirmed = loaded.Value.Confirm();
        if (confirmed.IsFailure)
        {
            return confirmed;
        }

        // GoodsReceiptConfirmed ke Outbox GRConfirmed. commit satu SaveChanges.
        await translator.TranslateAndClearAsync(loaded.Value, cancellationToken);
        return confirmed;
    }
}
